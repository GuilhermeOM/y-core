using System.Collections.Concurrent;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions;
using Y.Threads.Domain.DomainEvents;
using Y.Threads.Domain.Entities;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Models;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.Services;

namespace Y.Threads.Application.Posts.UseCases.CreatePost;
internal sealed class CreatePostUseCaseHandler : IUseCaseHandler<CreatePostUseCase, Guid>
{
    private readonly IPostRepository _postRepository;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;
    private readonly IStorageService _storageService;

    public CreatePostUseCaseHandler(
        IPostRepository postRepository,
        IDomainEventsDispatcher domainEventsDispatcher,
        IStorageService storageService)
    {
        _postRepository = postRepository;
        _domainEventsDispatcher = domainEventsDispatcher;
        _storageService = storageService;
    }

    public async Task<Result<Guid>> HandleAsync(CreatePostUseCase request, CancellationToken cancellationToken = default)
    {
        var post = new Post
        {
            Author = request.Author,
            Text = request.Text,
            Parent = request.Parent
        };

        var medias = await UploadMediasAsync(request.Author.Id, request.Medias, cancellationToken);
        if (!medias.Any())
        {
            return Result.Failure<Guid>(PostErrors.MediaUploadFailed);
        }

        post.Medias = medias;

        var id = await _postRepository.CreateAsync(post, cancellationToken);
        if (id == Guid.Empty)
        {
            return Result.Failure<Guid>(PostErrors.PostCreationFailed);
        }

        await _domainEventsDispatcher.DispatchAsync(
        [
            new CreateThreadDomainEvent(post.Id, GetThreadType(post.Parent))
        ], cancellationToken);

        return Result.Success(id);
    }

    private async Task<IEnumerable<Media>> UploadMediasAsync(
        Guid userId,
        ICollection<CreateMediaPost> files,
        CancellationToken cancellationToken)
    {
        var medias = new ConcurrentBag<Media>();

        try
        {
            var uploadTasks = files.Select(file => UploadMediaAsync(userId, file, medias, cancellationToken));

            var uploadTasksResult = await Task.WhenAll(uploadTasks);
            if (uploadTasksResult.Any(result => result is false))
            {
                await RollbackMediasUploadAsync(userId, medias);
                return [];
            }

            return medias;
        }
        catch
        {
            await RollbackMediasUploadAsync(userId, medias);
            return [];
        }
    }

    private async Task<bool> UploadMediaAsync(
        Guid userId,
        CreateMediaPost file,
        ConcurrentBag<Media> medias,
        CancellationToken cancellationToken)
    {
        using var stream = file.Media.OpenReadStream();

        var uploadedMedia = await _storageService.UploadMediaAsync(userId, stream, cancellationToken);
        if (uploadedMedia is null)
        {
            return false;
        }
        uploadedMedia.Description = file.Description;

        medias.Add(uploadedMedia);
        return true;
    }

    private async Task RollbackMediasUploadAsync(Guid userId, IEnumerable<Media> medias)
    {
        var deleteMediaTasks = medias
            .Select(media => _storageService.DeleteMediaAsync(userId, media));

        await Task.WhenAll(deleteMediaTasks);
    }

    private static ThreadType GetThreadType(Guid parentId) => parentId == Guid.Empty ? ThreadType.Post : ThreadType.Reply;
}

using Microsoft.AspNetCore.Http;
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
            Medias = [],
            Parent = request.Parent
        };

        foreach (var media in request.Medias)
        {
            var uploadedMedia = await UploadMediaAsync(media, request.Author.Id);
            if (uploadedMedia is null)
            {
                return Result.Failure<Guid>(PostErrors.MediaUploadFailed);
            }

            post.Medias = post.Medias.Append(uploadedMedia);
        }

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

    private async Task<Media?> UploadMediaAsync(IFormFile file, Guid userId)
    {
        using var stream = file.OpenReadStream();
        return await _storageService.UploadMediaAsync(userId, stream);
    }

    private static ThreadType GetThreadType(Guid parentId) => parentId == Guid.Empty ? ThreadType.Post : ThreadType.Reply;
}

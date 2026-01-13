using Microsoft.Extensions.Logging;
using Serilog.Context;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.Services;

namespace Y.Threads.Application.Posts.Commands.CreatePost;
internal sealed class CreatePostCommandHandler : ICommandHandler<CreatePostCommand, Guid>
{
    private readonly ILogger<CreatePostCommandHandler> _logger;
    private readonly IPostRepository _postRepository;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;
    private readonly IStorageService _storageService;

    public CreatePostCommandHandler(
        ILogger<CreatePostCommandHandler> logger,
        IPostRepository postRepository,
        IDomainEventsDispatcher domainEventsDispatcher,
        IStorageService storageService)
    {
        _logger = logger;
        _postRepository = postRepository;
        _domainEventsDispatcher = domainEventsDispatcher;
        _storageService = storageService;
    }

    public async Task<Result<Guid>> HandleAsync(CreatePostCommand command, CancellationToken cancellationToken = default)
    {
        using var _ = LogContext.PushProperty("AuthorId", command.Author.Id);

        var mediaUploadManager = new CreatePostMediaUploadManager(_storageService);

        try
        {
            var uploadedMedias = await mediaUploadManager.UploadManyAsync(command.Author.Id, command.Medias, cancellationToken);
            if (uploadedMedias.Count != command.Medias.Count)
            {
                return Result.Failure<Guid>(PostErrors.MediaUploadFailed);
            }

            var postCreationResult = Post.Create(command.Author, command.Text, uploadedMedias);
            if (postCreationResult.IsFailure)
            {
                await mediaUploadManager.RollbackAsync(command.Author.Id);
                return Result.Failure<Guid>(postCreationResult.Error);
            }

            var postId = await _postRepository.CreateAsync(postCreationResult.Value, cancellationToken);
            if (postId == Guid.Empty)
            {
                await mediaUploadManager.RollbackAsync(command.Author.Id);
                return Result.Failure<Guid>(PostErrors.PostCreationFailed);
            }

            await _domainEventsDispatcher.DispatchAsync(postCreationResult.Value.GetDomainEvents(), cancellationToken);

            _logger.LogInformation("Post {PostId} successfully created", postId);
            return Result.Success(postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error ocurred while creating the post for author {AuthorId}", command.Author.Id);

            await mediaUploadManager.RollbackAsync(command.Author.Id);
            return Result.Failure<Guid>(PostErrors.PostCreationFailed);
        }
    }
}

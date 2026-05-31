using Microsoft.Extensions.Logging;
using Serilog.Context;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Posts.Services.CreatePostMedia;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Application.Posts.Commands.CreatePost;
internal sealed class CreatePostCommandHandler : ICommandHandler<CreatePostCommand, Guid>
{
    private IReadOnlyCollection<FileUploadResult> _mediaUploadResults = [];

    private readonly ILogger<CreatePostCommandHandler> _logger;
    private readonly IPostRepository _postRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;
    private readonly ICreatePostMediaService _createPostMediaService;

    public CreatePostCommandHandler(
        ILogger<CreatePostCommandHandler> logger,
        IPostRepository postRepository,
        IUnitOfWork unitOfWork,
        IDomainEventsDispatcher domainEventsDispatcher,
        ICreatePostMediaService createPostMediaService)
    {
        _logger = logger;
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
        _domainEventsDispatcher = domainEventsDispatcher;
        _createPostMediaService = createPostMediaService;
    }

    public async Task<Result<Guid>> HandleAsync(CreatePostCommand command, CancellationToken cancellationToken = default)
    {
        using var _ = LogContext.PushProperty("AuthorId", command.Author.Id);

        try
        {
            var uploadedMediasResult = await _createPostMediaService.UploadManyAsync(command.Author.Id, command.Medias, cancellationToken);
            if (uploadedMediasResult.IsFailure)
            {
                return Result.Failure<Guid>(uploadedMediasResult.Error);
            }

            _mediaUploadResults = uploadedMediasResult.Value;

            var postCreationResult = Post.Create(command.Author, command.Text, _mediaUploadResults);
            if (postCreationResult.IsFailure)
            {
                await _createPostMediaService.RollbackAsync(_mediaUploadResults);
                return Result.Failure<Guid>(postCreationResult.Error);
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var postId = await _postRepository.CreateAsync(postCreationResult.Value, cancellationToken);
            if (postId == Guid.Empty)
            {
                await _createPostMediaService.RollbackAsync(_mediaUploadResults);
                return Result.Failure<Guid>(PostErrors.PostCreationFailed);
            }

            await transaction.CommitAsync(cancellationToken);
            await _domainEventsDispatcher.DispatchAsync(postCreationResult.Value.GetDomainEvents(), cancellationToken);

            _logger.LogInformation("Post {PostId} successfully created", postId);
            return Result.Success(postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error ocurred while creating the post for author {AuthorId}", command.Author.Id);

            await _createPostMediaService.RollbackAsync(_mediaUploadResults);
            return Result.Failure<Guid>(PostErrors.PostCreationFailed);
        }
    }
}

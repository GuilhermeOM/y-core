using Microsoft.Extensions.Logging;
using Serilog.Context;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Posts.Services.CreatePostMedia;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Application.Posts.Commands.ReplyPost;

internal sealed class ReplyPostCommandHandler : ICommandHandler<ReplyPostCommand, Guid>
{
    private IReadOnlyCollection<FileUploadResult> _mediaUploadResults = [];

    private readonly ILogger<ReplyPostCommandHandler> _logger;
    private readonly IPostRepository _postRepository;
    private readonly ICreatePostMediaService _createPostMediaService;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;

    public ReplyPostCommandHandler(
        ILogger<ReplyPostCommandHandler> logger,
        IPostRepository postRepository,
        ICreatePostMediaService createPostMediaService,
        IDomainEventsDispatcher domainEventsDispatcher)
    {
        _logger = logger;
        _postRepository = postRepository;
        _createPostMediaService = createPostMediaService;
        _domainEventsDispatcher = domainEventsDispatcher;
    }

    public async Task<Result<Guid>> HandleAsync(ReplyPostCommand command, CancellationToken cancellationToken = default)
    {
        using var _ = LogContext.PushProperty("AuthorId", command.Author.Id);
        using var __ = LogContext.PushProperty("ParentPostId", command.Parent);

        var post = await _postRepository.GetByIdAsync(command.Parent, cancellationToken);
        if (post is null)
        {
            return Result.Failure<Guid>(PostErrors.PostNotFound);
        }

        try
        {
            var uploadedMediasResult = await _createPostMediaService.UploadManyAsync(command.Author.Id, command.Medias, cancellationToken);
            if (uploadedMediasResult.IsFailure)
            {
                return Result.Failure<Guid>(uploadedMediasResult.Error);
            }

            _mediaUploadResults = uploadedMediasResult.Value;

            var reply = post.Reply(command.Author, command.Text, uploadedMediasResult.Value);
            if (reply.IsFailure)
            {
                await _createPostMediaService.RollbackAsync(_mediaUploadResults);
                return Result.Failure<Guid>(reply.Error);
            }

            var replyId = await _postRepository.CreateAsync(reply.Value, cancellationToken);
            if (replyId == Guid.Empty)
            {
                await _createPostMediaService.RollbackAsync(_mediaUploadResults);
                return Result.Failure<Guid>(PostErrors.PostReplyCreationFailed);
            }

            await _domainEventsDispatcher.DispatchAsync(reply.Value.GetDomainEvents(), cancellationToken);

            _logger.LogInformation("Reply {ReplyId} successfully created", reply.Value.Id);
            return Result.Success(reply.Value.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a reply by author {AuthorId}", command.Author.Id);

            await _createPostMediaService.RollbackAsync(_mediaUploadResults);
            return Result.Failure<Guid>(PostErrors.PostReplyCreationFailed);
        }
    }
}

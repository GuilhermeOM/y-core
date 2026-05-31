using Microsoft.Extensions.Logging;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.DomainEvents;
internal sealed class PostLikedDomainEventHandler : IDomainEventHandler<PostLikedEvent>
{
    private readonly ILogger<PostLikedDomainEventHandler> _logger;
    private readonly IPostRepository _postRepository;
    private readonly IThreadRepository _threadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PostLikedDomainEventHandler(
        ILogger<PostLikedDomainEventHandler> logger,
        IPostRepository postRepository,
        IThreadRepository threadRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _postRepository = postRepository;
        _threadRepository = threadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(PostLikedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var postLikeResult = PostLike.Create(domainEvent.PostId, domainEvent.UserId);
        if (postLikeResult.IsFailure)
        {
            _logger.LogError("Failure creating post like. Error {@Error}", postLikeResult.Error);
            return;
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _postRepository.TryCreatePostLikeAsync(postLikeResult.Value, cancellationToken);
            await _threadRepository.IncrementLikeAsync(domainEvent.PostId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostExceptions.PostAlreadyLikedException ex)
        {
            _logger.LogError(ex, "User {UserId} has already liked the current post {PostId}", domainEvent.UserId, domainEvent.PostId);
            return;
        }

        _logger.LogInformation("User {UserId} sucessfully liked the post {PostId}", domainEvent.UserId, domainEvent.PostId);
    }
}

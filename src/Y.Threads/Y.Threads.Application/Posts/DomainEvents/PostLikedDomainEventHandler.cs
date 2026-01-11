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
    private readonly IPostLikeRepository _postLikeRepository;
    private readonly IThreadRepository _threadRepository;

    public PostLikedDomainEventHandler(
        ILogger<PostLikedDomainEventHandler> logger,
        IPostLikeRepository postLikeRepository,
        IThreadRepository threadRepository)
    {
        _logger = logger;
        _postLikeRepository = postLikeRepository;
        _threadRepository = threadRepository;
    }

    public async Task HandleAsync(PostLikedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var postLikeResult = PostLike.Create(domainEvent.PostId, domainEvent.UserId);
        if (postLikeResult.IsFailure)
        {
            return;
        }

        try
        {
            await _postLikeRepository.TryCreateAsync(postLikeResult.Value, cancellationToken);
        }
        catch (PostExceptions.PostAlreadyLikedException ex)
        {
            _logger.LogError(ex, "User {UserId} has already liked the current post {PostId}", domainEvent.UserId, domainEvent.PostId);
            return;
        }

        await _threadRepository.IncrementLikeAsync(domainEvent.PostId, cancellationToken);

        _logger.LogInformation("User {UserId} sucessfully liked the post {PostId}", domainEvent.UserId, domainEvent.PostId);
    }
}

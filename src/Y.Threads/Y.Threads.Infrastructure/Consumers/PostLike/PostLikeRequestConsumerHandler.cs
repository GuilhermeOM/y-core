using KafkaFlow;
using Microsoft.Extensions.Logging;
using RedLockNet;
using Serilog.Context;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Infrastructure.Consumers.PostLike;
internal sealed class PostLikeRequestConsumerHandler : IMessageHandler<PostLikeRequestEvent>
{
    private readonly ILogger<PostLikeRequestConsumerHandler> _logger;
    private readonly IPostRepository _postRepository;
    private readonly IDistributedLockFactory _redisLock;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;

    public PostLikeRequestConsumerHandler(
        ILogger<PostLikeRequestConsumerHandler> logger,
        IPostRepository postRepository,
        IDistributedLockFactory redisLock,
        IDomainEventsDispatcher domainEventsDispatcher)
    {
        _logger = logger;
        _postRepository = postRepository;
        _redisLock = redisLock;
        _domainEventsDispatcher = domainEventsDispatcher;
    }

    public async Task Handle(IMessageContext context, PostLikeRequestEvent message)
    {
        using var _ = LogContext.PushProperty("PostId", message.PostId);
        using var __ = LogContext.PushProperty("UserId", message.UserId);

        var lockName = $"threads.{message.UserId}-{message.PostId}.lock";

        await using (var redLock = await _redisLock.CreateLockAsync(
            resource: lockName,
            expiryTime: TimeSpan.FromSeconds(30),
            waitTime: TimeSpan.FromSeconds(10),
            retryTime: TimeSpan.FromSeconds(1)))
        {
            if (!redLock.IsAcquired)
            {
                _logger.LogWarning("Lock not acquired. UserId: {UserId}, PostId: {PostId}", message.UserId, message.PostId);
                return;
            }

            var post = await _postRepository.GetByIdAsync(message.PostId);
            if (post is null)
            {
                _logger.LogError("Post not found. Like action can not be completed");
                return;
            }

            var likePost = post.Like(message.UserId);
            if (likePost.IsFailure)
            {
                _logger.LogError("Post can not be liked. Error {@Error}", likePost.Error);
                return;
            }

            await _domainEventsDispatcher.DispatchAsync(post.GetDomainEvents());
        }
    }
}

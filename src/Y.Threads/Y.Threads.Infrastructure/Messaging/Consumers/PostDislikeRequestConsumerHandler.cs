using KafkaFlow;
using Microsoft.Extensions.Logging;
using RedLockNet;
using Serilog.Context;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Constants;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Infrastructure.Messaging.Consumers;
internal sealed class PostDislikeRequestConsumerHandler : IMessageHandler<PostDislikeRequestEvent>
{
    private readonly ILogger<PostDislikeRequestConsumerHandler> _logger;
    private readonly IPostRepository _postRepository;
    private readonly IDistributedLockFactory _redisLock;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;

    public PostDislikeRequestConsumerHandler(
        ILogger<PostDislikeRequestConsumerHandler> logger,
        IPostRepository postRepository,
        IDistributedLockFactory redisLock,
        IDomainEventsDispatcher domainEventsDispatcher)
    {
        _logger = logger;
        _postRepository = postRepository;
        _redisLock = redisLock;
        _domainEventsDispatcher = domainEventsDispatcher;
    }

    public async Task Handle(IMessageContext context, PostDislikeRequestEvent message)
    {
        using var _ = LogContext.PushProperty("PostId", message.PostId);
        using var __ = LogContext.PushProperty("UserId", message.UserId);

        var lockName = RedisConstants.Lock.GetPostOperationLockName(message.UserId, message.PostId);

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
                _logger.LogError("Post not found. Dislike action can not be completed");
                return;
            }

            post.Dislike(message.UserId);

            await _domainEventsDispatcher.DispatchAsync(post.GetDomainEvents());
        }
    }
}

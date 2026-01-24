using FluentAssertions;
using KafkaFlow;
using Microsoft.Extensions.Logging;
using Moq;
using RedLockNet;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;
using Y.Threads.Infrastructure.Messaging.Consumers.PostLike;

namespace Y.Core.UnitTest.Y.Threads.Consumers;

public class PostLikeConsumerHandlerTests
{
    private readonly Mock<ILogger<PostLikeRequestConsumerHandler>> _loggerMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<IDistributedLockFactory> _redisLock;
    private readonly Mock<IDomainEventsDispatcher> _domainEventDispatcherMock;

    private readonly Mock<IMessageContext> _messageContext;
    private readonly Mock<IRedLock> _lockMock;

    private readonly PostLikeRequestConsumerHandler _handler;

    public PostLikeConsumerHandlerTests()
    {
        _loggerMock = new Mock<ILogger<PostLikeRequestConsumerHandler>>();
        _postRepositoryMock = new Mock<IPostRepository>();
        _redisLock = new Mock<IDistributedLockFactory>();
        _domainEventDispatcherMock = new Mock<IDomainEventsDispatcher>();
        _messageContext = new Mock<IMessageContext>();
        _lockMock = new Mock<IRedLock>();

        _handler = new PostLikeRequestConsumerHandler(
            _loggerMock.Object,
            _postRepositoryMock.Object,
            _redisLock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldNotExecute_WhenLockNotAcquired()
    {
        // Arrange
        var message = new PostLikeRequestEvent(Guid.NewGuid(), Guid.NewGuid());

        _redisLock
            .Setup(mock => mock.CreateLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                default))
            .ReturnsAsync(_lockMock.Object);

        _lockMock.SetupGet(mock => mock.IsAcquired).Returns(false);

        // Act
        await _handler.Handle(_messageContext.Object, message);

        // Assert
        _postRepositoryMock
            .Verify(mock => mock.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _domainEventDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPostNotFound()
    {
        // Arrange
        var message = new PostLikeRequestEvent(Guid.NewGuid(), Guid.NewGuid());

        _redisLock
            .Setup(mock => mock.CreateLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                default))
            .ReturnsAsync(_lockMock.Object);

        _lockMock.SetupGet(mock => mock.IsAcquired).Returns(true);

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(message.PostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Post));

        // Act
        await _handler.Handle(_messageContext.Object, message);

        // Assert
        _postRepositoryMock
            .Verify(mock => mock.GetByIdAsync(message.PostId, It.IsAny<CancellationToken>()), Times.Once);

        _domainEventDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPostStatusIsNotPublished()
    {
        // Arrange
        var message = new PostLikeRequestEvent(Guid.NewGuid(), Guid.NewGuid());

        _redisLock
            .Setup(mock => mock.CreateLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                default))
            .ReturnsAsync(_lockMock.Object);

        _lockMock.SetupGet(mock => mock.IsAcquired).Returns(true);

        var post = CreateDummyPost();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(message.PostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        post.Hide();

        // Act
        await _handler.Handle(_messageContext.Object, message);

        // Assert
        _postRepositoryMock
            .Verify(mock => mock.GetByIdAsync(message.PostId, It.IsAny<CancellationToken>()), Times.Once);

        _domainEventDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSucceed()
    {
        // Arrange
        var message = new PostLikeRequestEvent(Guid.NewGuid(), Guid.NewGuid());

        _redisLock
            .Setup(mock => mock.CreateLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                default))
            .ReturnsAsync(_lockMock.Object);

        _lockMock.SetupGet(mock => mock.IsAcquired).Returns(true);

        var post = CreateDummyPost();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(message.PostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        _domainEventDispatcherMock
            .Setup(mock => mock.DispatchAsync(post.GetDomainEvents(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(_messageContext.Object, message);

        // Assert
        post.GetDomainEvents().Should().Contain(x => x.GetType().Name == typeof(PostLikedEvent).Name);

        _postRepositoryMock
            .Verify(mock => mock.GetByIdAsync(message.PostId, It.IsAny<CancellationToken>()), Times.Once);

        _domainEventDispatcherMock
            .Verify(mock => mock.DispatchAsync(post.GetDomainEvents(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Post CreateDummyPost()
    {
        return Post.Create(new Author { Id = Guid.NewGuid() }, "text").Value;
    }
}

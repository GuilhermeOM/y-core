
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Y.Threads.Application.Posts.DomainEvents;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Core.UnitTest.Y.Threads.Posts.DomainEvents;
public class PostLikedDomainEventHandlerTests
{
    private readonly Mock<ILogger<PostLikedDomainEventHandler>> _loggerMock;
    private readonly Mock<IPostLikeRepository> _postLikeRepositoryMock;
    private readonly Mock<IThreadRepository> _threadRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock;

    private readonly PostLikedDomainEventHandler _handler;

    public PostLikedDomainEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<PostLikedDomainEventHandler>>();
        _postLikeRepositoryMock = new Mock<IPostLikeRepository>();
        _threadRepositoryMock = new Mock<IThreadRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _mongoClientMock = new Mock<IMongoClient>();
        _clientSessionHandleMock = new Mock<IClientSessionHandle>();

        _mongoClientMock
            .Setup(mock => mock.StartSessionAsync(default, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_clientSessionHandleMock.Object);

        _handler = new PostLikedDomainEventHandler(
            _loggerMock.Object,
            _postLikeRepositoryMock.Object,
            _threadRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenPostIdIsEmpty()
    {
        // Arrange
        var domainEvent = new PostLikedEvent(Guid.Empty, Guid.NewGuid());

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postLikeRepositoryMock
            .Verify(mock => mock.TryCreateAsync(It.IsAny<PostLike>(), It.IsAny<CancellationToken>()), Times.Never);

        _threadRepositoryMock
            .Verify(mock => mock.IncrementLikeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var domainEvent = new PostLikedEvent(Guid.NewGuid(), Guid.Empty);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postLikeRepositoryMock
            .Verify(mock => mock.TryCreateAsync(It.IsAny<PostLike>(), It.IsAny<CancellationToken>()), Times.Never);

        _threadRepositoryMock
            .Verify(mock => mock.IncrementLikeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_PostAlreadyLiked()
    {
        // Arrange
        var domainEvent = new PostLikedEvent(Guid.NewGuid(), Guid.NewGuid());

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _postLikeRepositoryMock
            .Setup(mock => mock.TryCreateAsync(
                It.Is<PostLike>(x => x.UserId == domainEvent.UserId
                    && x.PostId == domainEvent.PostId),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PostExceptions.PostAlreadyLikedException(""));

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postLikeRepositoryMock
            .Verify(mock => mock.TryCreateAsync(
                It.Is<PostLike>(x => x.UserId == domainEvent.UserId
                    && x.PostId == domainEvent.PostId),
                It.IsAny<CancellationToken>()), Times.Once);

        _threadRepositoryMock
            .Verify(mock => mock.IncrementLikeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        transactionMock
            .Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed()
    {
        // Arrange
        var domainEvent = new PostLikedEvent(Guid.NewGuid(), Guid.NewGuid());

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _postLikeRepositoryMock
            .Setup(mock => mock.TryCreateAsync(
                It.Is<PostLike>(x => x.UserId == domainEvent.UserId
                    && x.PostId == domainEvent.PostId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        _threadRepositoryMock
            .Setup(mock => mock.IncrementLikeAsync(domainEvent.PostId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postLikeRepositoryMock
            .Verify(mock => mock.TryCreateAsync(
                It.Is<PostLike>(x => x.UserId == domainEvent.UserId
                    && x.PostId == domainEvent.PostId),
                It.IsAny<CancellationToken>()), Times.Once);

        _threadRepositoryMock
            .Verify(mock => mock.IncrementLikeAsync(domainEvent.PostId, It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

}

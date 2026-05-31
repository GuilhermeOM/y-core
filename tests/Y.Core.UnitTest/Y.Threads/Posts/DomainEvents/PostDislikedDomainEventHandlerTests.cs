using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Y.Threads.Application.Posts.DomainEvents;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Core.UnitTest.Y.Threads.Posts.DomainEvents;
public class PostDislikedDomainEventHandlerTests
{
    private readonly Mock<ILogger<PostDislikedDomainEventHandler>> _loggerMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<IThreadRepository> _threadRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock;

    private readonly PostDislikedDomainEventHandler _handler;

    public PostDislikedDomainEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<PostDislikedDomainEventHandler>>();
        _postRepositoryMock = new Mock<IPostRepository>();
        _threadRepositoryMock = new Mock<IThreadRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _mongoClientMock = new Mock<IMongoClient>();
        _clientSessionHandleMock = new Mock<IClientSessionHandle>();

        _mongoClientMock
            .Setup(mock => mock.StartSessionAsync(default, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_clientSessionHandleMock.Object);

        _handler = new PostDislikedDomainEventHandler(
            _loggerMock.Object,
            _postRepositoryMock.Object,
            _threadRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldStop_WhenDeleteCountIsZero()
    {
        // Arrange
        var domainEvent = new PostDislikedEvent(Guid.NewGuid(), Guid.NewGuid());

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _postRepositoryMock
            .Setup(mock => mock.DeletePostLikeByPostIdUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postRepositoryMock
            .Verify(mock => mock.DeletePostLikeByPostIdUserIdAsync(domainEvent.PostId, domainEvent.UserId, It.IsAny<CancellationToken>()), Times.Once);

        _threadRepositoryMock
            .Verify(mock => mock.DecrementLikeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        transactionMock
            .Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldDislikePost()
    {
        // Arrange
        var domainEvent = new PostDislikedEvent(Guid.NewGuid(), Guid.NewGuid());

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _postRepositoryMock
            .Setup(mock => mock.DeletePostLikeByPostIdUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _threadRepositoryMock
            .Setup(mock => mock.DecrementLikeAsync(domainEvent.PostId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postRepositoryMock
            .Verify(mock => mock.DeletePostLikeByPostIdUserIdAsync(domainEvent.PostId, domainEvent.UserId, It.IsAny<CancellationToken>()), Times.Once);

        _threadRepositoryMock
            .Verify(mock => mock.DecrementLikeAsync(domainEvent.PostId, It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

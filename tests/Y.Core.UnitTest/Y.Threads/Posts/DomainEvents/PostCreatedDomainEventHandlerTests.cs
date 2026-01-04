using Moq;
using Y.Core.SharedKernel.Models;
using Y.Threads.Application.Posts.DomainEvents;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.ValueObjects;

using ThreadModel = Y.Threads.Application.Threads.Models.Thread;

namespace Y.Core.UnitTest.Y.Threads.Posts.DomainEvents;
public class PostCreatedDomainEventHandlerTests
{
    private readonly Mock<IThreadRepository> _threadRepositoryMock;

    private readonly PostCreatedDomainEventHandler _handler;

    public PostCreatedDomainEventHandlerTests()
    {
        _threadRepositoryMock = new Mock<IThreadRepository>();

        _handler = new PostCreatedDomainEventHandler(_threadRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateThread()
    {
        // Arrange
        var post = Post.Create(
            Guid.NewGuid(),
            "Dummy text",
            [new MediaUpload(string.Empty, string.Empty, "image/jpeg")]);

        var domainEvent = new PostCreatedEvent(
            post.Value.Id,
            new Author() { Id = post.Value.AuthorId, },
            post.Value.Text,
            post.Value.Medias);

        var media = post.Value.Medias.First();

        _threadRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<ThreadModel>(thread => thread.Text == domainEvent.Text
                    && thread.Depth == 0
                    && thread.Author.Id == domainEvent.Author.Id
                    && thread.Medias.Count == domainEvent.Medias.Count
                    && thread.Id == domainEvent.PostId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainEvent.PostId);

        // Act
        await _handler.HandleAsync(domainEvent, default);

        // Assert
        _threadRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.Is<ThreadModel>(thread => thread.Text == domainEvent.Text
                    && thread.Depth == 0
                    && thread.Author.Id == domainEvent.Author.Id
                    && thread.Medias.Count == domainEvent.Medias.Count
                    && thread.Id == domainEvent.PostId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

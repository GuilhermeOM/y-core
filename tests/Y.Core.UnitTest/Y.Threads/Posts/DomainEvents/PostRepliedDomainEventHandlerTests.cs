using FluentAssertions;
using Moq;
using Y.Core.SharedKernel.Models;
using Y.Threads.Application.Posts.DomainEvents;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Application.Threads.Models;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.ValueObjects;

using ThreadModel = Y.Threads.Application.Threads.Models.Thread;

namespace Y.Core.UnitTest.Y.Threads.Posts.DomainEvents;

public class PostRepliedDomainEventHandlerTests
{
    private readonly Mock<IThreadRepository> _threadRepositoryMock;

    private readonly PostRepliedDomainEventHandler _handler;

    public PostRepliedDomainEventHandlerTests()
    {
        _threadRepositoryMock = new Mock<IThreadRepository>();

        _handler = new PostRepliedDomainEventHandler(_threadRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenParentThreadDoesNotExist()
    {
        // Arrange
        var domainEvent = new PostRepliedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Author { Id = Guid.NewGuid() },
            "Test reply",
            []);

        _threadRepositoryMock
            .Setup(mock => mock.GetByIdAsync(domainEvent.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThreadModel?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(domainEvent, default));

        _threadRepositoryMock
            .Verify(mock => mock.CreateAsync(It.IsAny<ThreadModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateSubThread_WhenReplyHasTextOnly()
    {
        // Arrange
        var parentThread = new ThreadModel(
            Guid.NewGuid(),
            new Author { Id = Guid.NewGuid() },
            "Parent thread",
            [],
            depth: 0);

        var domainEvent = new PostRepliedEvent(
            Guid.NewGuid(),
            parentThread.Id,
            new Author { Id = Guid.NewGuid() },
            "Test reply",
            []);

        _threadRepositoryMock
            .Setup(mock => mock.GetByIdAsync(domainEvent.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentThread);

        _threadRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Id == domainEvent.PostId
                    && t.Author.Id == domainEvent.Author.Id
                    && t.Text == domainEvent.Text
                    && t.Medias.Count == 0
                    && t.Depth == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainEvent.PostId);

        // Act
        await _handler.HandleAsync(domainEvent, default);

        // Assert
        _threadRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Id == domainEvent.PostId
                    && t.Author.Id == domainEvent.Author.Id
                    && t.Text == domainEvent.Text
                    && t.Medias.Count == 0
                    && t.Depth == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateSubThread_WhenReplyHasMediasOnly()
    {
        // Arrange
        var parentThread = new ThreadModel(
            Guid.NewGuid(),
            new Author { Id = Guid.NewGuid() },
            "Parent thread",
            [],
            depth: 2);

        var fileUploadResults = new FileUploadResult[]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };

        var post = Post.Create(new Author { Id = Guid.NewGuid() }, string.Empty, fileUploadResults);

        var domainEvent = new PostRepliedEvent(
            Guid.NewGuid(),
            parentThread.Id,
            new Author { Id = Guid.NewGuid() },
            string.Empty,
            post.Value.Medias);

        _threadRepositoryMock
            .Setup(mock => mock.GetByIdAsync(domainEvent.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentThread);

        _threadRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Id == domainEvent.PostId
                    && t.Author.Id == domainEvent.Author.Id
                    && t.Text == domainEvent.Text
                    && t.Medias.Count == 1
                    && t.Depth == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainEvent.PostId);

        // Act
        await _handler.HandleAsync(domainEvent, default);

        // Assert
        _threadRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Id == domainEvent.PostId
                    && t.Author.Id == domainEvent.Author.Id
                    && t.Text == domainEvent.Text
                    && t.Medias.Count == 1
                    && t.Depth == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateSubThread_WhenReplyHasTextAndMedias()
    {
        // Arrange
        var parentThread = new ThreadModel(
            Guid.NewGuid(),
            new Author { Id = Guid.NewGuid() },
            "Parent thread",
            [],
            depth: 5);

        var fileUploadResults = new FileUploadResult[]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "video/mp4", "")
        };

        var post = Post.Create(new Author { Id = Guid.NewGuid() }, string.Empty, fileUploadResults);

        var domainEvent = new PostRepliedEvent(
            Guid.NewGuid(),
            parentThread.Id,
            new Author { Id = Guid.NewGuid() },
            "Test reply with media",
            post.Value.Medias);

        _threadRepositoryMock
            .Setup(mock => mock.GetByIdAsync(domainEvent.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentThread);

        _threadRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Id == domainEvent.PostId
                    && t.Author.Id == domainEvent.Author.Id
                    && t.Text == domainEvent.Text
                    && t.Medias.Count == 2
                    && t.Depth == 6),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainEvent.PostId);

        // Act
        await _handler.HandleAsync(domainEvent, default);

        // Assert
        _threadRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Id == domainEvent.PostId
                    && t.Author.Id == domainEvent.Author.Id
                    && t.Text == domainEvent.Text
                    && t.Medias.Count == 2
                    && t.Depth == 6),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapMediasCorrectly()
    {
        // Arrange
        var parentThread = new ThreadModel(
            Guid.NewGuid(),
            new Author { Id = Guid.NewGuid() },
            "Parent thread",
            [],
            depth: 0);

        var blobId = Guid.NewGuid();
        var url = "http://dummy.com/image.jpg";
        var description = "Test description";
        var mime = "image/png";

        var fileUploadResults = new FileUploadResult[]
        {
            new(blobId, url, "path/", mime, description)
        };

        var post = Post.Create(new Author { Id = Guid.NewGuid() }, "Dummy text", fileUploadResults);
        var media = post.Value.Medias.First();

        var domainEvent = new PostRepliedEvent(
            Guid.NewGuid(),
            parentThread.Id,
            new Author { Id = Guid.NewGuid() },
            "Test reply",
            [media]);

        _threadRepositoryMock
            .Setup(mock => mock.GetByIdAsync(domainEvent.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentThread);

        _threadRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Medias.Count == 1
                    && t.Medias.First().MediaId == media.Id
                    && t.Medias.First().Url == media.Url
                    && t.Medias.First().Description == media.Description
                    && t.Medias.First().Mime == media.Mime
                    && t.Medias.First().Type == Media.GetMediaTypeByMime(media.Mime)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainEvent.PostId);

        // Act
        await _handler.HandleAsync(domainEvent, default);

        // Assert
        _threadRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.Is<ThreadModel>(t =>
                    t.Medias.Count == 1
                    && t.Medias.First().MediaId == media.Id
                    && t.Medias.First().Url == media.Url
                    && t.Medias.First().Description == media.Description
                    && t.Medias.First().Mime == media.Mime
                    && t.Medias.First().Type == Media.GetMediaTypeByMime(media.Mime)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;

namespace Y.Core.UnitTest.Y.Threads.Posts.Commands;

public class CreatePostCommandHandlerTests
{
    private readonly Mock<ILogger<CreatePostCommandHandler>> _loggerMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<IDomainEventsDispatcher> _domainEventsDispatcherMock;
    private readonly Mock<IStorageService> _storageServiceMock;

    private readonly Mock<IFormFile> _file0Mock;
    private readonly Mock<IFormFile> _file1Mock;

    private readonly CreatePostCommandHandler _handler;

    public CreatePostCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CreatePostCommandHandler>>();
        _postRepositoryMock = new Mock<IPostRepository>();
        _domainEventsDispatcherMock = new Mock<IDomainEventsDispatcher>();
        _storageServiceMock = new Mock<IStorageService>();

        _file0Mock = new Mock<IFormFile>();
        _file1Mock = new Mock<IFormFile>();

        var file0Data = "file0";
        var file0ByteArray = Encoding.UTF8.GetBytes(file0Data);
        var file0Stream = new MemoryStream(file0ByteArray);

        var file1Data = "file1";
        var file1ByteArray = Encoding.UTF8.GetBytes(file1Data);
        var file1Stream = new MemoryStream(file1ByteArray);

        _file0Mock.Setup(mock => mock.OpenReadStream()).Returns(file0Stream);
        _file1Mock.Setup(mock => mock.OpenReadStream()).Returns(file1Stream);

        _handler = new CreatePostCommandHandler(
            _loggerMock.Object,
            _postRepositoryMock.Object,
            _domainEventsDispatcherMock.Object,
            _storageServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenAllMediaUploadFails()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaUpload?)null);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.MediaUploadFailed);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                It.IsAny<Guid>(),
                It.IsAny<MediaUpload>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenSomeMediaUploadFails()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        var medias = command.Medias.ToList();
        using var media0Stream = medias[0].Media.OpenReadStream();
        using var media1Stream = medias[1].Media.OpenReadStream();

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media0Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaUpload?)null);

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media1Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUpload("", "", ""));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.MediaUploadFailed);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                command.Author.Id,
                It.IsAny<MediaUpload>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenAuthorIsEmpty()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ],
            Author = new Author()
        };

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUpload(string.Empty, string.Empty, string.Empty));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.EmptyAuthor);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                command.Author.Id,
                It.IsAny<MediaUpload>()),
            Times.Exactly(command.Medias.Count));
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenPostIsEmpty()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = string.Empty,
            Medias = [],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.EmptyPost);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                It.IsAny<Guid>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                It.IsAny<Guid>(),
                It.IsAny<MediaUpload>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenMaxMediaExceeds()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty),
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty),
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUpload(string.Empty, string.Empty, "image/jpeg"));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.MaxMediaExceeded);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                command.Author.Id,
                It.IsAny<MediaUpload>()),
            Times.Exactly(command.Medias.Count));
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenMaxMediaTypeIsNotSupported()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty),
            ],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUpload(string.Empty, string.Empty, "abc"));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.UnsupportedMediaType);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                command.Author.Id,
                It.IsAny<MediaUpload>()),
            Times.Exactly(command.Medias.Count));
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCreatePostInDatabaseFails()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty),
            ],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        var medias = command.Medias.ToList();
        using var media0Stream = medias[0].Media.OpenReadStream();
        using var media1Stream = medias[1].Media.OpenReadStream();

        var mediaUploads = new List<MediaUpload>()
        {
            new(string.Empty, string.Empty, "image/jpeg"),
            new(string.Empty, string.Empty, "image/jpeg")
        };

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media0Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaUploads[0]);

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media1Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaUploads[1]);

        var postResultMock = Post.Create(command.Author.Id, command.Text, mediaUploads);

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Empty);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.PostCreationFailed);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                command.Author.Id,
                It.IsAny<MediaUpload>()),
            Times.Exactly(command.Medias.Count));
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenCreatingMediaPost()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = string.Empty,
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        var medias = command.Medias.ToList();
        using var media0Stream = medias[0].Media.OpenReadStream();
        using var media1Stream = medias[1].Media.OpenReadStream();

        var mediaUploads = new List<MediaUpload>()
        {
            new(string.Empty, string.Empty, "image/jpeg"),
            new(string.Empty, string.Empty, "image/jpeg")
        };

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media0Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaUploads[0]);

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media1Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaUploads[1]);

        var postResultMock = Post.Create(command.Author.Id, command.Text, mediaUploads);
        var postId = Guid.NewGuid();

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postId);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<List<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postId, postResultMock.Value)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(postId);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                It.IsAny<Guid>(),
                It.IsAny<MediaUpload>()),
            Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenCreatingTextPost()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias = [],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        var postResultMock = Post.Create(command.Author.Id, command.Text);
        var postId = Guid.NewGuid();

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postId);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<List<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postId, postResultMock.Value)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(postId);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                It.IsAny<Guid>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                It.IsAny<Guid>(),
                It.IsAny<MediaUpload>()),
            Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenCreatingTextMediaPost()
    {
        // Arrange
        var command = new CreatePostCommand
        {
            Text = "Test content",
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ],
            Author = new Author
            {
                Id = Guid.NewGuid()
            }
        };

        var medias = command.Medias.ToList();
        using var media0Stream = medias[0].Media.OpenReadStream();
        using var media1Stream = medias[1].Media.OpenReadStream();

        var mediaUploads = new List<MediaUpload>()
        {
            new(string.Empty, string.Empty, "image/jpeg"),
            new(string.Empty, string.Empty, "image/jpeg")
        };

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media0Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaUploads[0]);

        _storageServiceMock
            .Setup(mock => mock.UploadMediaAsync(
                command.Author.Id,
                media1Stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaUploads[1]);

        var postResultMock = Post.Create(command.Author.Id, command.Text, mediaUploads);
        var postId = Guid.NewGuid();

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postId);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<List<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postId, postResultMock.Value)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(postId);

        _storageServiceMock.Verify(
            mock => mock.UploadMediaAsync(
                command.Author.Id,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(command.Medias.Count));

        _storageServiceMock.Verify(
            mock => mock.DeleteMediaAsync(
                It.IsAny<Guid>(),
                It.IsAny<MediaUpload>()),
            Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    private static bool AreDomainEventsWellDispatched(List<IDomainEvent> events, Guid postId, Post post)
    {
        var postCreatedEvent = events.OfType<PostCreatedEvent>().FirstOrDefault();

        return events.Count == 1
            && postCreatedEvent?.PostId == postId
            && postCreatedEvent?.Text == post.Text
            && postCreatedEvent?.Author.Id == post.AuthorId
            && postCreatedEvent?.Medias.Count == post.Medias.Count;
    }
}

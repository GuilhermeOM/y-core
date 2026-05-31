using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Application.Posts.Services.CreatePostMedia;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.ValueObjects;

namespace Y.Core.UnitTest.Y.Threads.Posts.Commands;
public class CreatePostCommandHandlerTests
{
    private readonly Mock<ILogger<CreatePostCommandHandler>> _loggerMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventsDispatcher> _domainEventsDispatcherMock;
    private readonly Mock<ICreatePostMediaService> _createPostMediaServiceMock;

    private readonly Mock<IFormFile> _file0Mock;
    private readonly Mock<IFormFile> _file1Mock;

    private readonly CreatePostCommandHandler _handler;

    public CreatePostCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CreatePostCommandHandler>>();
        _postRepositoryMock = new Mock<IPostRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventsDispatcherMock = new Mock<IDomainEventsDispatcher>();
        _createPostMediaServiceMock = new Mock<ICreatePostMediaService>();

        _file0Mock = new Mock<IFormFile>();
        _file1Mock = new Mock<IFormFile>();

        _handler = new CreatePostCommandHandler(
            _loggerMock.Object,
            _postRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventsDispatcherMock.Object,
            _createPostMediaServiceMock.Object);
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

        var expectedFailure = Result.Failure<FileUploadResult[]>(PostErrors.UnsupportedMediaType);

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFailure);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(expectedFailure.Error);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.IsAny<Post>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _domainEventsDispatcherMock.Verify(
            mock => mock.DispatchAsync(
                It.IsAny<List<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<FileUploadResult[]>([]));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.EmptyAuthor);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.IsAny<Post>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _domainEventsDispatcherMock.Verify(
            mock => mock.DispatchAsync(
                It.IsAny<List<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        var uploadedMedias = Array.Empty<FileUploadResult>();

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uploadedMedias));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.EmptyPost);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(uploadedMedias), Times.Once);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.IsAny<Post>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _domainEventsDispatcherMock.Verify(
            mock => mock.DispatchAsync(
                It.IsAny<List<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
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

        var uploadedMedias = new FileUploadResult[6]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uploadedMedias));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.MaxMediaExceeded);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(uploadedMedias), Times.Once);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.IsAny<Post>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _domainEventsDispatcherMock.Verify(
            mock => mock.DispatchAsync(
                It.IsAny<List<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        var uploadedMedias = new FileUploadResult[2]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "abc", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "abc", "")
        };

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uploadedMedias));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.UnsupportedMediaType);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(uploadedMedias), Times.Once);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock.Verify(
            mock => mock.CreateAsync(
                It.IsAny<Post>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _domainEventsDispatcherMock.Verify(
            mock => mock.DispatchAsync(
                It.IsAny<List<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        var uploadedMedias = new FileUploadResult[2]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uploadedMedias));

        var postResultMock = Post.Create(command.Author, command.Text, uploadedMedias);

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

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(uploadedMedias), Times.Once);

        _domainEventsDispatcherMock.Verify(
            mock => mock.DispatchAsync(
                It.IsAny<List<IDomainEvent>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        var uploadedMedias = new FileUploadResult[2]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uploadedMedias));

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        var postResultMock = Post.Create(command.Author, command.Text, uploadedMedias);

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postResultMock.Value.Id);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postResultMock.Value)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(postResultMock.Value.Id);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(It.IsAny<IReadOnlyCollection<FileUploadResult>>()), Times.Never);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()), Times.Once);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postResultMock.Value)),
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

        var uploadedMedias = Array.Empty<FileUploadResult>();

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uploadedMedias));

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        var postResultMock = Post.Create(command.Author, command.Text, uploadedMedias);

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postResultMock.Value.Id);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postResultMock.Value)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(postResultMock.Value.Id);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(It.IsAny<IReadOnlyCollection<FileUploadResult>>()), Times.Never);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()), Times.Once);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postResultMock.Value)),
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

        var uploadedMedias = new FileUploadResult[2]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };

        _createPostMediaServiceMock
            .Setup(mock => mock.UploadManyAsync(
                command.Author.Id,
                command.Medias,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(uploadedMedias));

        var transactionMock = new Mock<ITransactionScope>();

        _unitOfWorkMock
            .Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        var postResultMock = Post.Create(command.Author, command.Text, uploadedMedias);

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(postResultMock.Value.Id);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postResultMock.Value)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(postResultMock.Value.Id);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(It.IsAny<IReadOnlyCollection<FileUploadResult>>()), Times.Never);

        _unitOfWorkMock
            .Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(post => post.AuthorId == postResultMock.Value.AuthorId
                    && post.Text == postResultMock.Value.Text
                    && post.Medias.Count == postResultMock.Value.Medias.Count
                    && post.Status == postResultMock.Value.Status),
                It.IsAny<CancellationToken>()), Times.Once);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => AreDomainEventsWellDispatched(events, postResultMock.Value)),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    private static bool AreDomainEventsWellDispatched(IReadOnlyList<IDomainEvent> events, Post post)
    {
        var postCreatedEvent = events.OfType<PostCreatedEvent>().FirstOrDefault();

        return events.Count == 1
            && postCreatedEvent?.Text == post.Text
            && postCreatedEvent?.Author.Id == post.AuthorId
            && postCreatedEvent?.Medias.Count == post.Medias.Count;
    }
}

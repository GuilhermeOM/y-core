using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Application.Posts.Commands.ReplyPost;
using Y.Threads.Application.Posts.Services.CreatePostMedia;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.ValueObjects;

namespace Y.Core.UnitTest.Y.Threads.Posts.Commands;

public class ReplyPostCommandHandlerTests
{
    private readonly Mock<ILogger<ReplyPostCommandHandler>> _loggerMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventsDispatcher> _domainEventsDispatcherMock;
    private readonly Mock<ICreatePostMediaService> _createPostMediaServiceMock;

    private readonly Mock<IFormFile> _file0Mock;
    private readonly Mock<IFormFile> _file1Mock;

    private readonly ReplyPostCommandHandler _handler;

    public ReplyPostCommandHandlerTests()
    {
        _loggerMock = new Mock<ILogger<ReplyPostCommandHandler>>();
        _postRepositoryMock = new Mock<IPostRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventsDispatcherMock = new Mock<IDomainEventsDispatcher>();
        _createPostMediaServiceMock = new Mock<ICreatePostMediaService>();

        _file0Mock = new Mock<IFormFile>();
        _file1Mock = new Mock<IFormFile>();

        _handler = new ReplyPostCommandHandler(
            _loggerMock.Object,
            _postRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _createPostMediaServiceMock.Object,
            _domainEventsDispatcherMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenParentPostNotFound()
    {
        // Arrange
        var command = new ReplyPostCommand
        {
            Parent = Guid.NewGuid(),
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias = []
        };

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.PostNotFound);

        _createPostMediaServiceMock
            .Verify(mock => mock.UploadManyAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<CreateMediaPost>>(), It.IsAny<CancellationToken>()), Times.Never);

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()), Times.Never);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenMediaUploadFails()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ]
        };

        var expectedFailure = Result.Failure<FileUploadResult[]>(PostErrors.UnsupportedMediaType);

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()), Times.Never);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenAuthorIsEmpty()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author(),
            Medias = []
        };

        var uploadedMedias = Array.Empty<FileUploadResult>();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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
        result.Error.Should().BeEquivalentTo(PostErrors.EmptyAuthor);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(uploadedMedias), Times.Once);

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()), Times.Never);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenReplyIsEmpty()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = string.Empty,
            Author = new Author { Id = Guid.NewGuid() },
            Medias = []
        };

        var uploadedMedias = Array.Empty<FileUploadResult>();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()), Times.Never);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenMaxMediaExceeds()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty),
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty),
                new(_file0Mock.Object, string.Empty),
            ]
        };

        var uploadedMedias = new FileUploadResult[5]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()), Times.Never);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUnsupportedMediaType()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
            ]
        };

        var uploadedMedias = new FileUploadResult[1]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "abc", "")
        };

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()), Times.Never);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenCreateReplyInDatabaseFails()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias = []
        };

        var uploadedMedias = Array.Empty<FileUploadResult>();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.IsAny<Post>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Empty);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.PostReplyCreationFailed);

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(uploadedMedias), Times.Once);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenExceptionOccurs()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
            ]
        };

        var uploadedMedias = new FileUploadResult[1]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.IsAny<Post>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.PostReplyCreationFailed);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(uploadedMedias), Times.Once);

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenCreatingTextReply()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias = []
        };

        var uploadedMedias = Array.Empty<FileUploadResult>();
        var expectedReplyId = Guid.NewGuid();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(p => p.Parent == parentPost.Id
                    && p.Text == command.Text
                    && p.AuthorId == command.Author.Id
                    && p.Medias.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReplyId);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => events.Any(e => e is PostRepliedEvent)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedReplyId);

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(It.IsAny<IReadOnlyCollection<FileUploadResult>>()), Times.Never);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(p => p.Parent == parentPost.Id
                    && p.Text == command.Text
                    && p.AuthorId == command.Author.Id
                    && p.Medias.Count == 0),
                It.IsAny<CancellationToken>()), Times.Once);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => events.Any(e => e is PostRepliedEvent)),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenCreatingMediaReply()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = string.Empty,
            Author = new Author { Id = Guid.NewGuid() },
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
                new(_file1Mock.Object, string.Empty)
            ]
        };

        var uploadedMedias = new FileUploadResult[2]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", ""),
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };
        var expectedReplyId = Guid.NewGuid();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(p => p.Parent == parentPost.Id
                    && p.Text == string.Empty
                    && p.AuthorId == command.Author.Id
                    && p.Medias.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReplyId);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => events.Any(e => e is PostRepliedEvent)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedReplyId);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(It.IsAny<IReadOnlyCollection<FileUploadResult>>()), Times.Never);

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(p => p.Parent == parentPost.Id
                    && p.Text == string.Empty
                    && p.AuthorId == command.Author.Id
                    && p.Medias.Count == 2),
                It.IsAny<CancellationToken>()), Times.Once);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => events.Any(e => e is PostRepliedEvent)),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenCreatingTextMediaReply()
    {
        // Arrange
        var parentPost = Post.Create(new Author { Id = Guid.NewGuid() }, "Parent post", []).Value;

        var command = new ReplyPostCommand
        {
            Parent = parentPost.Id,
            Text = "Test reply",
            Author = new Author { Id = Guid.NewGuid() },
            Medias =
            [
                new(_file0Mock.Object, string.Empty),
            ]
        };

        var uploadedMedias = new FileUploadResult[1]
        {
            new(Guid.NewGuid(), "http://dummy.com", "path/", "image/jpeg", "")
        };
        var expectedReplyId = Guid.NewGuid();

        _postRepositoryMock
            .Setup(mock => mock.GetByIdAsync(command.Parent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentPost);

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

        _postRepositoryMock
            .Setup(mock => mock.CreateAsync(
                It.Is<Post>(p => p.Parent == parentPost.Id
                    && p.Text == command.Text
                    && p.AuthorId == command.Author.Id
                    && p.Medias.Count == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReplyId);

        _domainEventsDispatcherMock
            .Setup(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => events.Any(e => e is PostRepliedEvent)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedReplyId);

        _createPostMediaServiceMock
            .Verify(mock => mock.RollbackAsync(It.IsAny<IReadOnlyCollection<FileUploadResult>>()), Times.Never);

        _unitOfWorkMock.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        transactionMock.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _postRepositoryMock
            .Verify(mock => mock.CreateAsync(
                It.Is<Post>(p => p.Parent == parentPost.Id
                    && p.Text == command.Text
                    && p.AuthorId == command.Author.Id
                    && p.Medias.Count == 1),
                It.IsAny<CancellationToken>()), Times.Once);

        _domainEventsDispatcherMock
            .Verify(mock => mock.DispatchAsync(
                It.Is<IReadOnlyList<IDomainEvent>>(events => events.Any(e => e is PostRepliedEvent)),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}

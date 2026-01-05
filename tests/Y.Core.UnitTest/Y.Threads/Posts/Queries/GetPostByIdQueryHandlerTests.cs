using FluentAssertions;
using Moq;
using Y.Threads.Application.Posts.Queries.GetPostById;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;

namespace Y.Core.UnitTest.Y.Threads.Posts.Queries;
public class GetPostByIdQueryHandlerTests
{
    private readonly Mock<IPostRepository> _postRepositoryMock;

    private readonly GetPostByIdQueryHandler _handler;

    public GetPostByIdQueryHandlerTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();

        _handler = new GetPostByIdQueryHandler(_postRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenPostDoesNotExist()
    {
        // Arrange
        var query = new GetPostByIdQuery(Guid.NewGuid());

        _postRepositoryMock
            .Setup(repo => repo.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        // Act
        var result = await _handler.HandleAsync(query, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.PostNotFound);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenPostExists()
    {
        // Arrange
        var query = new GetPostByIdQuery(Guid.NewGuid());

        var post = Post.Create(Guid.NewGuid(), "Dummy text").Value;

        _postRepositoryMock
            .Setup(repo => repo.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);

        // Act
        var result = await _handler.HandleAsync(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(post);
    }
}

using FluentAssertions;
using Moq;
using Y.Core.SharedKernel.Models;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Application.Threads.Models;
using Y.Threads.Application.Threads.Queries.GetThreadById;
using Y.Threads.Domain.Errors;

namespace Y.Core.UnitTest.Y.Threads.Threads.Queries;

public class GetThreadByIdQueryHandlerTests
{
    private readonly Mock<IThreadRepository> _threadRepositoryMock;

    private readonly GetThreadByIdQueryHandler _handler;

    public GetThreadByIdQueryHandlerTests()
    {
        _threadRepositoryMock = new Mock<IThreadRepository>();

        _handler = new GetThreadByIdQueryHandler(_threadRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenThreadDoesNotExist()
    {
        // Arrange
        var query = new GetThreadByIdQuery(Guid.NewGuid(), 1);

        _threadRepositoryMock
            .Setup(mock => mock.GetByIdAndMaxDepthAsync(
                query.Id,
                query.MaxDepth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.HandleAsync(query, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ThreadErrors.ThreadNotFound);

        _threadRepositoryMock
            .Verify(mock => mock.GetByIdAndMaxDepthAsync(
                query.Id,
                query.MaxDepth,
                It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenThreadExists()
    {
        // Arrange
        var query = new GetThreadByIdQuery(Guid.NewGuid(), 2);

        var threads = new GetThreadByIdQueryResponse[]
        {
            CreateThread(query.Id, depth: 0),
            CreateThread(Guid.NewGuid(), depth: 1),
            CreateThread(Guid.NewGuid(), depth: 2)
        };

        _threadRepositoryMock
            .Setup(mock => mock.GetByIdAndMaxDepthAsync(
                query.Id,
                query.MaxDepth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(threads);

        // Act
        var result = await _handler.HandleAsync(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(threads);

        _threadRepositoryMock
            .Verify(mock => mock.GetByIdAndMaxDepthAsync(
                query.Id,
                query.MaxDepth,
                It.IsAny<CancellationToken>()),
                Times.Once);
    }

    private static GetThreadByIdQueryResponse CreateThread(Guid id, long depth)
    {
        return new GetThreadByIdQueryResponse(
            id,
            new AuthorSnapshot(new Author
            {
                Id = Guid.NewGuid(),
                Name = "Dummy author",
                AvatarUrl = "http://dummy.com/avatar.png"
            }),
            "Dummy text",
            [],
            depth,
            LikeAmount: 10,
            ReplyAmount: 5,
            DateTime.UtcNow);
    }
}

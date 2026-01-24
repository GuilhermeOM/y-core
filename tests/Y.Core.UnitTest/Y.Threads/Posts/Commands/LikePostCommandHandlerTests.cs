using FluentAssertions;
using Moq;
using Y.Contract.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Posts.Commands.LikePost;
using Y.Threads.Domain.Constants;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Services;

namespace Y.Core.UnitTest.Y.Threads.Posts.Commands;
public class LikePostCommandHandlerTests
{
    private readonly Mock<IProducerService> _producerServiceMock;

    private readonly LikePostCommandHandler _handler;

    public LikePostCommandHandlerTests()
    {
        _producerServiceMock = new Mock<IProducerService>();

        _handler = new LikePostCommandHandler(_producerServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed()
    {
        // Arrange
        var command = new LikePostCommand
        {
            PostId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        _producerServiceMock
            .Setup(mock => mock.ProduceAsync(
                It.Is<PostLikeRequestEvent>(x =>
                    x.UserId == command.UserId
                    && x.PostId == command.PostId),
                It.Is<MessageMetadata>(x =>
                    x.MessageKey == command.UserId.ToString()
                    && x.Topic == KafkaConstants.Topics.PostLikeTopic)))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _producerServiceMock
            .Verify(mock => mock.ProduceAsync(
                It.Is<PostLikeRequestEvent>(x =>
                    x.UserId == command.UserId
                    && x.PostId == command.PostId),
                It.Is<MessageMetadata>(x =>
                    x.MessageKey == command.UserId.ToString()
                    && x.Topic == KafkaConstants.Topics.PostLikeTopic)), Times.Once);
    }
}

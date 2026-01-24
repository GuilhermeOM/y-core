using FluentAssertions;
using Moq;
using Y.Contract.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Posts.Commands.DislikePost;
using Y.Threads.Domain.Constants;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Services;

namespace Y.Core.UnitTest.Y.Threads.Posts.Commands;
public class DislikePostCommandHandlerTests
{
    private readonly Mock<IProducerService> _producerServiceMock;

    private readonly DislikePostCommandHandler _handler;

    public DislikePostCommandHandlerTests()
    {
        _producerServiceMock = new Mock<IProducerService>();

        _handler = new DislikePostCommandHandler(_producerServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed()
    {
        // Arrange
        var command = new DislikePostCommand
        {
            PostId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        _producerServiceMock
            .Setup(mock => mock.ProduceAsync(
                It.Is<PostDislikeRequestEvent>(x =>
                    x.UserId == command.UserId
                    && x.PostId == command.PostId),
                It.Is<MessageMetadata>(x =>
                    x.MessageKey == command.UserId.ToString()
                    && x.Topic == KafkaConstants.Topics.PostDislikeTopic)))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _producerServiceMock
            .Verify(mock => mock.ProduceAsync(
                It.Is<PostDislikeRequestEvent>(x =>
                    x.UserId == command.UserId
                    && x.PostId == command.PostId),
                It.Is<MessageMetadata>(x =>
                    x.MessageKey == command.UserId.ToString()
                    && x.Topic == KafkaConstants.Topics.PostDislikeTopic)), Times.Once);
    }
}

using KafkaFlow;
using KafkaFlow.Producers;
using Moq;
using Polly;
using Polly.Registry;
using Y.Contract.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Constants;
using Y.Threads.Infrastructure.Resilience;
using Y.Threads.Infrastructure.Services;

namespace Y.Core.UnitTest.Y.Threads.Services;
public class ProducerServiceTests
{
    private readonly Mock<IProducerAccessor> _producerAccessorMock;
    private readonly Mock<ResiliencePipelineProvider<string>> _resiliencePipelineProviderMock;

    private readonly ProducerService _service;

    public ProducerServiceTests()
    {
        _producerAccessorMock = new Mock<IProducerAccessor>();
        _resiliencePipelineProviderMock = new Mock<ResiliencePipelineProvider<string>>();

        _resiliencePipelineProviderMock
           .Setup(mock => mock.GetPipeline(It.Is<string>(x => x == Resiliences.FastDefaultRetryPipelinePolicy)))
           .Returns(ResiliencePipeline.Empty);

        _service = new ProducerService(_producerAccessorMock.Object, _resiliencePipelineProviderMock.Object);
    }

    [Fact]
    public async Task ProduceAsync_ShouldPublishMessage()
    {
        // Arrange
        var messageMetadata = new MessageMetadata
        {
            MessageKey = Guid.NewGuid().ToString(),
            Topic = Guid.NewGuid().ToString()
        };

        var message = new DummyMessage();
        var messageProducerMock = new Mock<IMessageProducer>();

        _producerAccessorMock
            .Setup(mock => mock[KafkaConstants.Producers.Threads])
            .Returns(messageProducerMock.Object);

        // Act
        await _service.ProduceAsync(message, messageMetadata);

        // Assert
        messageProducerMock.Verify(mock => mock
            .ProduceAsync(messageMetadata.Topic, messageMetadata.MessageKey, message, default, default), Times.Once);
    }

    private class DummyMessage : IKafkaMessage
    {
    }
}

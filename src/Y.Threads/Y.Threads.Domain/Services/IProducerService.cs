using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Services;
public interface IProducerService
{
    Task ProduceAsync(IKafkaMessage message, MessageMetadata metadata);
}

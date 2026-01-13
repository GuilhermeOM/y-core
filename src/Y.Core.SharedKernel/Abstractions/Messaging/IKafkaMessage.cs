namespace Y.Core.SharedKernel.Abstractions.Messaging;

public interface IKafkaMessage;

public sealed record MessageMetadata
{
    public required string MessageKey { get; init; }
    public required string ProducerName { get; init; }
}

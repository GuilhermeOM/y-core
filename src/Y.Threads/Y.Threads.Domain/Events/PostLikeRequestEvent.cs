using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Events;
public sealed record PostLikeRequestEvent : IKafkaMessage
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
}

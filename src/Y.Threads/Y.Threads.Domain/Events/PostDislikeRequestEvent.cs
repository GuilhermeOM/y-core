using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Events;

public sealed record PostDislikeRequestEvent(Guid PostId, Guid UserId) : IKafkaMessage;

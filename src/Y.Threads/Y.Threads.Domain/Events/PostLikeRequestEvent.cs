using Y.Contract.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Events;

public sealed record PostLikeRequestEvent(Guid PostId, Guid UserId) : IKafkaMessage;

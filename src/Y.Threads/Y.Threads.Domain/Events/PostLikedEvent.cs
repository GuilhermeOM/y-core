using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Events;

public sealed record PostLikedEvent(Guid PostId, Guid UserId) : IDomainEvent;

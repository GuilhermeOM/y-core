using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Events;

public sealed record PostDislikedEvent(Guid PostId, Guid UserId) : IDomainEvent;

using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Entities;

namespace Y.Threads.Domain.DomainEvents;
public sealed record CreateThreadDomainEvent(Guid PostId, ThreadType ThreadType) : IDomainEvent;

using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Aggregates;
public abstract class Entity
{
    private readonly List<IDomainEvent> _events = [];

    public Guid Id { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _events.AsReadOnly();

    public void RaiseDomainEvent(IDomainEvent domainEvent) => _events.Add(domainEvent);

    public void ClearDomainEvents() => _events.Clear();
}

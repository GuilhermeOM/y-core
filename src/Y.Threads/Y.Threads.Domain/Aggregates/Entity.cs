using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Domain.Aggregates;
public abstract class Entity
{
    private List<IDomainEvent> _events = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<IDomainEvent> GetDomainEvents()
    {
        _events ??= [];
        return _events.AsReadOnly();
    }

    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _events ??= [];
        _events.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _events?.Clear();
    }
}

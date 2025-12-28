namespace Y.Threads.Domain.Aggregates;
public abstract class Entity
{
    public Guid Id { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}

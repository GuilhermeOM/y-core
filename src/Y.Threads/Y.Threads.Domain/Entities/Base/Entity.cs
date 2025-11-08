namespace Y.Threads.Domain.Entities.Base;
public class Entity
{
    public Guid Id { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}


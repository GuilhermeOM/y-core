namespace Y.Threads.Domain.Models;
public class Media
{
    public Guid Id { get; set; }
    public required string Url { get; set; }
    public string Description { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MediaType
{
    Image = 0,
    Video = 1
}

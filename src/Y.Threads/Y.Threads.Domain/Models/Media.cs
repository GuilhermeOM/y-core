namespace Y.Threads.Domain.Models;
public class Media
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MediaType
{
    Unknown = 0,
    Image = 1,
    Video = 2
}

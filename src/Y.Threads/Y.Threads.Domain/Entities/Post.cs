using Y.Threads.Domain.Entities.Base;
using Y.Threads.Domain.Models;

namespace Y.Threads.Domain.Entities;
public class Post : Entity
{
    public Guid ThreadId { get; set; }
    public required Author Author { get; set; }
    public string Text { get; set; } = string.Empty;
    public IEnumerable<Media> Medias { get; set; } = [];
    public Guid Parent { get; set; } = Guid.Empty;
    public long LikeAmount { get; set; }
    public long ReplyAmount { get; set; }
}

public class Author
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

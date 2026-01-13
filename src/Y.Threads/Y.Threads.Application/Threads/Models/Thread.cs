using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Models;
using Y.Threads.Domain.Aggregates;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Application.Threads.Models;

public sealed class Thread : Entity
{
    public Author Author { get; init; } = null!;
    public string Text { get; init; } = string.Empty;
    public IReadOnlyCollection<MediaSnapshot> Medias { get; init; } = [];
    public long Depth { get; init; }
    public long LikeAmount { get; init; }
    public long ReplyAmount { get; init; }

    public Thread(
        Guid postId,
        Author author,
        string text,
        IReadOnlyCollection<MediaSnapshot> medias)
    {
        Id = postId;
        Author = author;
        Text = text;
        Medias = medias;
    }
}

public sealed class MediaSnapshot
{
    public Guid MediaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Mime { get; set; } = string.Empty;
    public MediaType Type { get; set; }
}

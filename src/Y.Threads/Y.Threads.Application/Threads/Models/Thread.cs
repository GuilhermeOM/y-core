using Y.Core.SharedKernel.Models;
using Y.Threads.Domain.Aggregates;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Application.Threads.Models;

public sealed class Thread : Entity
{
    public Guid CorrelationId { get; init; }
    public AuthorSnapshot Author { get; init; } = null!;
    public string Text { get; init; } = string.Empty;
    public IReadOnlyCollection<MediaSnapshot> Medias { get; init; } = [];
    public long Depth { get; init; }
    public long LikeAmount { get; init; }
    public long ReplyAmount { get; init; }

    public Thread(
        Guid postId,
        Author author,
        string text,
        IReadOnlyCollection<Media> medias,
        long depth = 0)
    {
        Id = postId;
        Author = new AuthorSnapshot(author);
        Text = text;
        Medias = [.. medias.Select(media => new MediaSnapshot(media))];
        Depth = depth;
    }
}

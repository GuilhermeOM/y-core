using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Domain.Events;
public sealed record PostCreatedEvent : IDomainEvent
{
    public Guid PostId { get; set; }
    public Author Author { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public IReadOnlyCollection<Media> Medias { get; set; } = [];

    public PostCreatedEvent(Guid postId, Author author, string text, IReadOnlyCollection<Media> medias)
    {
        PostId = postId;
        Author = author;
        Text = text;
        Medias = medias;
    }
}

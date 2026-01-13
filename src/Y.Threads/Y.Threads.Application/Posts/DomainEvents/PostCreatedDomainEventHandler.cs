using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Application.Threads.Models;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Events;

namespace Y.Threads.Application.Posts.DomainEvents;
internal sealed class PostCreatedDomainEventHandler : IDomainEventHandler<PostCreatedEvent>
{
    private readonly IThreadRepository _threadRepository;

    public PostCreatedDomainEventHandler(IThreadRepository threadRepository)
    {
        _threadRepository = threadRepository;
    }

    public async Task HandleAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var thread = new Threads.Models.Thread(
            domainEvent.PostId,
            domainEvent.Author,
            domainEvent.Text,
            [.. domainEvent.Medias.Select(ToMediaSnapshot)]);

        await _threadRepository.CreateAsync(thread, cancellationToken);
    }

    public static MediaSnapshot ToMediaSnapshot(Media media)
    {
        return new MediaSnapshot
        {
            MediaId = media.Id,
            Name = media.Name,
            Url = media.Url,
            Description = media.Description,
            Mime = media.Mime,
            Type = Media.GetMediaTypeByMime(media.Mime)
        };
    }
}

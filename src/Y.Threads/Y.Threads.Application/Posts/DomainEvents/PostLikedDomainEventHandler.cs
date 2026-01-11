using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Events;

namespace Y.Threads.Application.Posts.DomainEvents;
internal sealed class PostLikedDomainEventHandler : IDomainEventHandler<PostLikedEvent>
{
    public async Task HandleAsync(PostLikedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        
    }
}

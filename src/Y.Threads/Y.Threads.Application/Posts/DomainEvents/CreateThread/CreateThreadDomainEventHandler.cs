using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.DomainEvents;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.DomainEvents.CreateThread;
internal sealed class CreateThreadDomainEventHandler : IDomainEventHandler<CreateThreadDomainEvent>
{
    private readonly IThreadRepository _threadRepository;

    public CreateThreadDomainEventHandler(IThreadRepository threadRepository)
    {
        _threadRepository = threadRepository;
    }

    public async Task HandleAsync(CreateThreadDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var thread = new Domain.Entities.Thread
        {
            PostId = domainEvent.PostId,
            Type = domainEvent.ThreadType,
        };

        await _threadRepository.CreateAsync(thread, cancellationToken);
    }
}

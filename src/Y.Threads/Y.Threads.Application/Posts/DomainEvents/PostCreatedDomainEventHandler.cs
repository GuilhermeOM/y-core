using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Application.Threads.Models;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.DomainEvents;
internal sealed class PostCreatedDomainEventHandler : IDomainEventHandler<PostCreatedEvent>
{
    private readonly IThreadRepository _threadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PostCreatedDomainEventHandler(IThreadRepository threadRepository, IUnitOfWork unitOfWork)
    {
        _threadRepository = threadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(PostCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var thread = new Threads.Models.Thread(
            domainEvent.PostId,
            domainEvent.Author,
            domainEvent.Text,
            domainEvent.Medias)
        {
            CorrelationId = domainEvent.PostId
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        await _threadRepository.CreateAsync(thread, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

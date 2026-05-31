using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Application.Threads.Models;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.DomainEvents;

internal sealed class PostRepliedDomainEventHandler : IDomainEventHandler<PostRepliedEvent>
{
    private readonly IThreadRepository _threadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PostRepliedDomainEventHandler(IThreadRepository threadRepository, IUnitOfWork unitOfWork)
    {
        _threadRepository = threadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(PostRepliedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var parent = await _threadRepository.GetByIdAsync(domainEvent.Parent, cancellationToken)
            ?? throw new InvalidOperationException("A thread must exists in order to create a sub thread");

        var subThread = new Threads.Models.Thread(
            domainEvent.PostId,
            domainEvent.Author,
            domainEvent.Text,
            domainEvent.Medias,
            parent.Depth + 1)
        {
            CorrelationId = parent.CorrelationId
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        await _threadRepository.IncrementReplyAsync(parent.Id, cancellationToken);
        await _threadRepository.CreateAsync(subThread, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}

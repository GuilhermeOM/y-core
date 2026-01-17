using Microsoft.Extensions.Logging;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.DomainEvents;
internal sealed class PostDislikedDomainEventHandler : IDomainEventHandler<PostDislikedEvent>
{
    private readonly ILogger<PostDislikedDomainEventHandler> _logger;
    private readonly IPostLikeRepository _postLikeRepository;
    private readonly IThreadRepository _threadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PostDislikedDomainEventHandler(
        ILogger<PostDislikedDomainEventHandler> logger,
        IPostLikeRepository postLikeRepository,
        IThreadRepository threadRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _postLikeRepository = postLikeRepository;
        _threadRepository = threadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(PostDislikedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var deleteAmount = await _postLikeRepository
            .DeleteByPostIdUserIdAsync(domainEvent.PostId, domainEvent.UserId, cancellationToken);

        if (deleteAmount == 0)
        {
            _logger.LogWarning("Can not dislike a post not liked");
            return;
        }

        await _threadRepository.DecrementLikeAsync(domainEvent.PostId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}

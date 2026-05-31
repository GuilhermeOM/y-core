using Y.Threads.Application.Threads.Queries.GetThreadById;

namespace Y.Threads.Application.Threads.Abstractions;
public interface IThreadRepository
{
    Task<Models.Thread?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GetThreadByIdQueryResponse>> GetByIdAndMaxDepthAsync(Guid id, int maxDepth, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Models.Thread thread, CancellationToken cancellationToken = default);
    Task IncrementReplyAsync(Guid id, CancellationToken cancellationToken = default);
    Task IncrementLikeAsync(Guid id, CancellationToken cancellationToken = default);
    Task DecrementLikeAsync(Guid id, CancellationToken cancellationToken = default);
}

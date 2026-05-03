
namespace Y.Threads.Application.Threads.Abstractions;
public interface IThreadRepository
{
    Task<Models.Thread> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Models.Thread thread, CancellationToken cancellationToken = default);
    Task IncrementLikeAsync(Guid id, CancellationToken cancellationToken = default);
    Task DecrementLikeAsync(Guid id, CancellationToken cancellationToken = default);
}

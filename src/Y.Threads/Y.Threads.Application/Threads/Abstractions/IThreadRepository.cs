namespace Y.Threads.Application.Threads.Abstractions;
public interface IThreadRepository
{
    Task<Guid> CreateAsync(Models.Thread thread, CancellationToken cancellationToken = default);
    Task IncrementLikeAsync(Guid id, CancellationToken cancellationToken = default);
}

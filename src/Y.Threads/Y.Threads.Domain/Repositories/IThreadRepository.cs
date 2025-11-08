namespace Y.Threads.Domain.Repositories;
public interface IThreadRepository
{
    Task<Guid> CreateAsync(Entities.Thread post, CancellationToken cancellationToken = default);
}

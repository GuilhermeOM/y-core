using Y.Threads.Domain.Entities;

namespace Y.Threads.Domain.Repositories;
public interface IPostRepository
{
    Task<Guid> CreateAsync(Post post, CancellationToken cancellationToken = default);
}

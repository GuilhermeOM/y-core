using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Domain.Repositories;
public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Post post, CancellationToken cancellationToken = default);
}

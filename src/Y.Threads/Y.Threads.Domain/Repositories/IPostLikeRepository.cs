using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Domain.Repositories;

public interface IPostLikeRepository
{
    Task<Guid> TryCreateAsync(PostLike postLike, CancellationToken cancellationToken = default);
    Task<long> DeleteByPostIdUserIdAsync(
        Guid postId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

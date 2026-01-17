using MongoDB.Driver;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Infrastructure.Persistence.Repositories;
internal sealed class PostLikeRepository : IPostLikeRepository
{
    private readonly AppDataContext _context;

    public PostLikeRepository(AppDataContext context)
    {
        _context = context;
    }

    public async Task<Guid> TryCreateAsync(PostLike postLike, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.PostLikes.InsertOneAsync(postLike, default, cancellationToken);
            return postLike.Id;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new PostExceptions.PostAlreadyLikedException("Post already liked");
        }
    }

    public async Task<long> DeleteByPostIdUserIdAsync(
        Guid postId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.PostLikes
            .DeleteOneAsync(postLike => postLike.PostId == postId && postLike.UserId == userId, cancellationToken);

        return result.DeletedCount;
    }
}

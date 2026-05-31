using MongoDB.Driver;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;
using Y.Threads.Infrastructure.Persistence.Abstractions;

namespace Y.Threads.Infrastructure.Persistence.Repositories;
internal sealed class PostRepository : IPostRepository
{
    private readonly AppDataContext _context;
    private readonly IMongoSessionAccessor _sessionAccessor;

    public PostRepository(AppDataContext context, IMongoSessionAccessor sessionAccessor)
    {
        _context = context;
        _sessionAccessor = sessionAccessor;
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Post>.Filter.Eq(post => post.Id, id);
        return await _context.Posts.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(Post post, CancellationToken cancellationToken = default)
    {
        await _context.Posts.InsertOneAsync(
            _sessionAccessor.ClientSessionHandle,
            post,
            default,
            cancellationToken);

        return post.Id;
    }

    public async Task<Guid> TryCreatePostLikeAsync(PostLike postLike, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.PostLikes.InsertOneAsync(
                _sessionAccessor.ClientSessionHandle,
                postLike,
                default,
                cancellationToken);

            return postLike.Id;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new PostExceptions.PostAlreadyLikedException("Post already liked");
        }
    }

    public async Task<long> DeletePostLikeByPostIdUserIdAsync(
        Guid postId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.PostLikes.DeleteOneAsync(
            _sessionAccessor.ClientSessionHandle,
            postLike => postLike.PostId == postId && postLike.UserId == userId,
            cancellationToken: cancellationToken);

        return result.DeletedCount;
    }
}

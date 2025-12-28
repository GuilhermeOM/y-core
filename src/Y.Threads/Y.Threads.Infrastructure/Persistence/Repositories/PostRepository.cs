using MongoDB.Driver;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Infrastructure.Persistence.Repositories;
internal sealed class PostRepository : IPostRepository
{
    private readonly AppDataContext _context;

    public PostRepository(AppDataContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Post>.Filter.Eq(post => post.Id, id);
        return await _context.Posts.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(Post post, CancellationToken cancellationToken = default)
    {
        await _context.Posts.InsertOneAsync(post, default, cancellationToken);
        return post.Id;
    }
}

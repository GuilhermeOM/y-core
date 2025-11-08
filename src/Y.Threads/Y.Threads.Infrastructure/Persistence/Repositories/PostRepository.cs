using Y.Threads.Domain.Entities;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Infrastructure.Persistence.Repositories;
internal sealed class PostRepository : IPostRepository
{
    private readonly AppDataContext _context;

    public PostRepository(AppDataContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateAsync(Post post, CancellationToken cancellationToken = default)
    {
        await _context.Posts.InsertOneAsync(post, default, cancellationToken);
        return post.Id;
    }
}

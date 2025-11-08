using Y.Threads.Domain.Repositories;

namespace Y.Threads.Infrastructure.Persistence.Repositories;
internal sealed class ThreadRepository : IThreadRepository
{
    private readonly AppDataContext _context;

    public ThreadRepository(AppDataContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateAsync(Domain.Entities.Thread post, CancellationToken cancellationToken = default)
    {
        await _context.Threads.InsertOneAsync(post, default, cancellationToken);
        return post.Id;
    }
}

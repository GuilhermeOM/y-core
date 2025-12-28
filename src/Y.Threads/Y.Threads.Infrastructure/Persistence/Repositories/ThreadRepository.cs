using Y.Threads.Application.Threads.Abstractions;

namespace Y.Threads.Infrastructure.Persistence.Repositories;
internal sealed class ThreadRepository : IThreadRepository
{
    private readonly AppDataContext _context;

    public ThreadRepository(AppDataContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateAsync(Application.Threads.Models.Thread thread, CancellationToken cancellationToken = default)
    {
        await _context.Threads.InsertOneAsync(thread, default, cancellationToken);
        return thread.Id;
    }
}

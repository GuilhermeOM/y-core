using MongoDB.Driver;
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

    public async Task IncrementLikeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Application.Threads.Models.Thread>.Filter.Eq(field => field.Id, id);
        var update = Builders<Application.Threads.Models.Thread>.Update.Inc(thread => thread.LikeAmount, 1);

        await _context.Threads.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}

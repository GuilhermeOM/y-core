using MongoDB.Driver;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Application.Threads.Models;
using Y.Threads.Application.Threads.Queries.GetThreadById;
using Y.Threads.Infrastructure.Persistence.Abstractions;

namespace Y.Threads.Infrastructure.Persistence.Repositories;
internal sealed class ThreadRepository : IThreadRepository
{
    private readonly AppDataContext _context;
    private readonly IMongoSessionAccessor _sessionAccessor;

    public ThreadRepository(AppDataContext context, IMongoSessionAccessor sessionAccessor)
    {
        _context = context;
        _sessionAccessor = sessionAccessor;
    }

    public async Task<Application.Threads.Models.Thread?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Application.Threads.Models.Thread>.Filter.Eq(thread => thread.Id, id);
        var cursor = await _context.Threads.FindAsync(filter, cancellationToken: cancellationToken);

        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<GetThreadByIdQueryResponse>> GetByIdAndMaxDepthAsync(Guid id, int maxDepth, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Application.Threads.Models.Thread>.Filter.Empty;

        filter &= Builders<Application.Threads.Models.Thread>.Filter.Eq(thread => thread.CorrelationId, id);
        filter &= Builders<Application.Threads.Models.Thread>.Filter.Lte(thread => thread.Depth, maxDepth);

        var sort = Builders<Application.Threads.Models.Thread>.Sort
            .Ascending(thread => thread.Depth)
            .Ascending(thread => thread.CreatedAt);

        var projection = Builders<Application.Threads.Models.Thread>.Projection.Expression(thread => new GetThreadByIdQueryResponse(
            thread.Id,
            thread.Author,
            thread.Text,
            thread.Medias,
            thread.Depth,
            thread.LikeAmount,
            thread.ReplyAmount,
            thread.CreatedAt
        ));

        return await _context.Threads
            .Find(filter)
            .Project(projection)
            .Sort(sort)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(Application.Threads.Models.Thread thread, CancellationToken cancellationToken = default)
    {
        await _context.Threads.InsertOneAsync(
            _sessionAccessor.ClientSessionHandle,
            thread,
            default,
            cancellationToken);

        return thread.Id;
    }

    public async Task IncrementReplyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Application.Threads.Models.Thread>.Filter.Eq(field => field.Id, id);
        var update = Builders<Application.Threads.Models.Thread>.Update.Inc(thread => thread.ReplyAmount, 1);

        await _context.Threads.UpdateOneAsync(
            _sessionAccessor.ClientSessionHandle,
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    public async Task IncrementLikeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Application.Threads.Models.Thread>.Filter.Eq(field => field.Id, id);
        var update = Builders<Application.Threads.Models.Thread>.Update.Inc(thread => thread.LikeAmount, 1);

        await _context.Threads.UpdateOneAsync(
            _sessionAccessor.ClientSessionHandle,
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    public async Task DecrementLikeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Application.Threads.Models.Thread>.Filter.Eq(field => field.Id, id);
        var update = Builders<Application.Threads.Models.Thread>.Update.Inc(thread => thread.LikeAmount, -1);

        await _context.Threads.UpdateOneAsync(
            _sessionAccessor.ClientSessionHandle,
            filter,
            update,
            cancellationToken: cancellationToken);
    }
}

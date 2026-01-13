using MongoDB.Driver;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Infrastructure.Persistence;
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly IMongoClient _mongoClient;

    public UnitOfWork(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await TransactionScope.CreateAsync(_mongoClient, cancellationToken);
    }
}

internal sealed class TransactionScope : ITransactionScope
{
    private readonly IClientSessionHandle _session;
    private bool _committed;

    private TransactionScope(IClientSessionHandle session)
    {
        _session = session;
    }

    internal static async Task<TransactionScope> CreateAsync(IMongoClient client, CancellationToken cancellationToken = default)
    {
        var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();

        return new TransactionScope(session);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _session.CommitTransactionAsync(cancellationToken);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            await _session.AbortTransactionAsync();
        }
        _session.Dispose();
    }
}

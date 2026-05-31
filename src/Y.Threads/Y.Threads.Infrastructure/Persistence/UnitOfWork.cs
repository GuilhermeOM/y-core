using Confluent.Kafka;
using MongoDB.Driver;
using Y.Threads.Domain.Repositories;
using Y.Threads.Infrastructure.Persistence.Abstractions;

namespace Y.Threads.Infrastructure.Persistence;
internal sealed class UnitOfWork : IUnitOfWork, IMongoSessionAccessor
{
    private readonly IMongoClient _mongoClient;

    public UnitOfWork(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public IClientSessionHandle? ClientSessionHandle { get; private set; }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ClientSessionHandle = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        ClientSessionHandle.StartTransaction();

        return new TransactionScope(ClientSessionHandle!, () => ClientSessionHandle = null);
    }
}

internal sealed class TransactionScope : ITransactionScope
{
    private readonly IClientSessionHandle _clientSessionHandle;
    private readonly Action _clearSessionAction;
    private bool _committed;

    internal TransactionScope(IClientSessionHandle clientSessionHandle, Action clearSessionAction)
    {
        _clientSessionHandle = clientSessionHandle;
        _clearSessionAction = clearSessionAction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _clientSessionHandle.CommitTransactionAsync(cancellationToken);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_committed)
            {
                await _clientSessionHandle.AbortTransactionAsync();
            }
        }
        finally
        {
            _clientSessionHandle.Dispose();
            _clearSessionAction();
        }
    }
}

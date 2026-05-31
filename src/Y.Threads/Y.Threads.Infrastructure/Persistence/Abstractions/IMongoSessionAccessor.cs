using MongoDB.Driver;

namespace Y.Threads.Infrastructure.Persistence.Abstractions;

internal interface IMongoSessionAccessor
{
    IClientSessionHandle? ClientSessionHandle { get; }
}

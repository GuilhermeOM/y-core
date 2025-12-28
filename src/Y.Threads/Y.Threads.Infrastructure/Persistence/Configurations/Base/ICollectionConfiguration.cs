using MongoDB.Driver;
using Y.Threads.Domain.Aggregates;

namespace Y.Threads.Infrastructure.Persistence.Configurations.Base;
internal interface ICollectionConfiguration<TCollection> where TCollection : Entity
{
    Task ConfigureAsync(IMongoCollection<TCollection> collection, CancellationToken cancellationToken = default);
}

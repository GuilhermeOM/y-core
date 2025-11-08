using MongoDB.Driver;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;

namespace Y.Threads.Infrastructure.Persistence.Configurations;
internal sealed class ThreadConfiguration : ICollectionConfiguration<Domain.Entities.Thread>
{
    public async Task ConfigureAsync(IMongoCollection<Domain.Entities.Thread> collection, CancellationToken cancellationToken = default)
    {
        var postIdIndex = Builders<Domain.Entities.Thread>.IndexKeys.Ascending(thread => thread.PostId);

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Domain.Entities.Thread>(postIdIndex)
        ], cancellationToken);
    }
}

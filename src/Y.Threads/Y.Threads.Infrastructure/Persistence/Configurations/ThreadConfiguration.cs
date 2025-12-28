using MongoDB.Driver;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;

namespace Y.Threads.Infrastructure.Persistence.Configurations;
internal sealed class ThreadConfiguration : ICollectionConfiguration<Application.Threads.Models.Thread>
{
    public async Task ConfigureAsync(IMongoCollection<Application.Threads.Models.Thread> collection, CancellationToken cancellationToken = default)
    {
        var authorIdIndex = Builders<Application.Threads.Models.Thread>.IndexKeys.Ascending(thread => thread.Author.Id);

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Application.Threads.Models.Thread>(authorIdIndex)
        ], cancellationToken);
    }
}

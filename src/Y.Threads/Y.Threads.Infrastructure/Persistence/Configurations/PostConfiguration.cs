using MongoDB.Driver;
using Y.Threads.Domain.Entities;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;

namespace Y.Threads.Infrastructure.Persistence.Configurations;
internal sealed class PostConfiguration : ICollectionConfiguration<Post>
{
    public async Task ConfigureAsync(IMongoCollection<Post> collection, CancellationToken cancellationToken = default)
    {
        var threadIdIndex = Builders<Post>.IndexKeys.Ascending(post => post.ThreadId);
        var authorIdIndex = Builders<Post>.IndexKeys.Ascending(post => post.Author.Id);

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Post>(threadIdIndex),
            new CreateIndexModel<Post>(authorIdIndex)
        ], cancellationToken);
    }
}

using MongoDB.Driver;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;

namespace Y.Threads.Infrastructure.Persistence.Configurations;
internal sealed class PostConfiguration : ICollectionConfiguration<Post>
{
    public async Task ConfigureAsync(IMongoCollection<Post> collection, CancellationToken cancellationToken = default)
    {
        var authorIdIndex = Builders<Post>.IndexKeys.Ascending(post => post.AuthorId);

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Post>(authorIdIndex)
        ], cancellationToken);
    }
}

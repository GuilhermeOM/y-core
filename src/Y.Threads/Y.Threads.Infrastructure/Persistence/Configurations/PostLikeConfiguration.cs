using MongoDB.Driver;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;

namespace Y.Threads.Infrastructure.Persistence.Configurations;

internal class PostLikeConfiguration : ICollectionConfiguration<PostLike>
{
    public async Task ConfigureAsync(IMongoCollection<PostLike> collection, CancellationToken cancellationToken = default)
    {
        var postUserIndex = Builders<PostLike>.IndexKeys
            .Ascending(thread => thread.PostId)
            .Ascending(thread => thread.UserId);

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<PostLike>(postUserIndex)
        ], cancellationToken);
    }
}

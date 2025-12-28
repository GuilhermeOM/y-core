using MongoDB.Driver;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Infrastructure.Persistence;
internal class AppDataContext
{
    public const string DatabaseName = "y";

    private readonly IMongoDatabase _mongoDatabase;

    public AppDataContext(IMongoClient mongoClient)
    {
        _mongoDatabase = mongoClient.GetDatabase(DatabaseName);
    }

    public IMongoCollection<Post> Posts => _mongoDatabase.GetCollection<Post>(nameof(Posts));
    public IMongoCollection<Application.Threads.Models.Thread> Threads => _mongoDatabase.GetCollection<Application.Threads.Models.Thread>(nameof(Threads));
}

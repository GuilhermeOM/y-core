using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Infrastructure.Persistence;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;

namespace Y.Threads.Infrastructure.Background;
internal sealed class MongoConfiguratorBackgroundService : IHostedService
{
    private readonly ILogger<MongoConfiguratorBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MongoConfiguratorBackgroundService(
        ILogger<MongoConfiguratorBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting mongo collections configuration");

        using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDataContext>();

        await scope.ServiceProvider
            .GetRequiredService<ICollectionConfiguration<Post>>()
            .ConfigureAsync(context.Posts, cancellationToken);

        await scope.ServiceProvider
            .GetRequiredService<ICollectionConfiguration<PostLike>>()
            .ConfigureAsync(context.PostLikes, cancellationToken);

        await scope.ServiceProvider
            .GetRequiredService<ICollectionConfiguration<Application.Threads.Models.Thread>>()
            .ConfigureAsync(context.Threads, cancellationToken);

        _logger.LogInformation("Mongo collections configuration successfully completed");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

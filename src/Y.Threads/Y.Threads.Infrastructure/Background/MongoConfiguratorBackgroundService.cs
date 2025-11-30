using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Y.Threads.Domain.Entities;
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
        _logger.LogInformation("Iniciando configuração das collections do mongo. Assembly {AssemblyName}", typeof(AssemblyReference).Name);

        using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDataContext>();

        await scope.ServiceProvider
            .GetRequiredService<ICollectionConfiguration<Post>>()
            .ConfigureAsync(context.Posts, cancellationToken);

        await scope.ServiceProvider
            .GetRequiredService<ICollectionConfiguration<Domain.Entities.Thread>>()
            .ConfigureAsync(context.Threads, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Finalizando configuração das collections do mongo. Assembly {AssemblyName}", typeof(AssemblyReference).Name);
        return Task.CompletedTask;
    }
}

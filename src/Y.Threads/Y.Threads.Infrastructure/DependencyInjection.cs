using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Y.Threads.Infrastructure.Background;
using Y.Threads.Infrastructure.Persistence;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;

namespace Y.Threads.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddPersistence(configuration).AddBackgroundServices();
    }

    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<MongoConfiguratorBackgroundService>();
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DatabaseConnection");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

        services.AddScoped(implementation =>
        {
            var mongoClient = implementation.GetRequiredService<IMongoClient>();
            return new AppDataContext(mongoClient);
        });

        services.Scan(scan => scan
            .FromAssembliesOf(typeof(AssemblyReference))
            .AddClasses(classes => classes.AssignableTo(typeof(ICollectionConfiguration<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}

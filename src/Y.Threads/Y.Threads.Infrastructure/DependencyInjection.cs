using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Y.Core.SharedKernel.Abstractions;
using Y.Threads.Domain.Repositories;
using Y.Threads.Infrastructure.Background;
using Y.Threads.Infrastructure.DomainEvents;
using Y.Threads.Infrastructure.Persistence;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;
using Y.Threads.Infrastructure.Persistence.Repositories;

namespace Y.Threads.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddPersistence(configuration)
            .AddRepositories()
            .AddBackgroundServices()
            .AddDomainEventsDispatcher();
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

        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IThreadRepository, ThreadRepository>();

        return services;
    }

    private static IServiceCollection AddDomainEventsDispatcher(this IServiceCollection services)
    {
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        return services;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeDetective;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Polly;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Domain.Repositories;
using Y.Threads.Domain.Services;
using Y.Threads.Infrastructure.Background;
using Y.Threads.Infrastructure.Consumers;
using Y.Threads.Infrastructure.DomainEvents;
using Y.Threads.Infrastructure.Persistence;
using Y.Threads.Infrastructure.Persistence.Configurations.Base;
using Y.Threads.Infrastructure.Persistence.Repositories;
using Y.Threads.Infrastructure.Resilience;
using Y.Threads.Infrastructure.Services;

namespace Y.Threads.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddPersistence(configuration)
            .AddRepositories()
            .AddBackgroundServices()
            .AddDomainEventsDispatcher()
            .AddSupabase(configuration)
            .AddFileInspector()
            .AddServices()
            .AddPipelinePolicies()
            .AddKafka(configuration)
            .AddRedis(configuration);
    }

    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<MongoConfiguratorBackgroundService>();
        services.AddHostedService<KafkaBusStartBackgroundService>();

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

    private static IServiceCollection AddSupabase(this IServiceCollection services, IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"];
        var supabaseKey = configuration["Supabase:Key"];

        services.AddSingleton(provider =>
        {
            return new Supabase.Client(supabaseUrl!, supabaseKey, new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = true
            });
        });

        return services;
    }

    private static IServiceCollection AddFileInspector(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            return new ContentInspectorBuilder()
            {
                Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
            }.Build();
        });

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IStorageService, StorageService>();
        services.AddScoped<IProducerService, ProducerService>();

        return services;
    }

    private static IServiceCollection AddPipelinePolicies(this IServiceCollection services)
    {
        services.AddResiliencePipeline(Resiliences.FastDefaultRetryPipelinePolicy, builder => ResilienceBuilder.FastDefaultRetryPipelinePolicy(builder));

        return services;
    }

    private static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureKafkaTopology(configuration);

        return services;
    }

    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var endpointsConfiguration = configuration.GetRequiredSection("Redis:Endpoints").Get<string[]>();
        var endpoints = new EndPointCollection();

        foreach (var endpoint in endpointsConfiguration!)
        {
            endpoints.Add(endpoint);
        }

        var options = new ConfigurationOptions
        {
            EndPoints = endpoints,
            ConnectRetry = 5,
            ReconnectRetryPolicy = new ExponentialRetry(500, 2000)
        };

        var connectionMultiplexer = ConnectionMultiplexer.Connect(options);

        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
        services.AddSingleton<IDistributedLockFactory>(provider =>
        {
            return RedLockFactory.Create(
            [
                new RedLockMultiplexer(connectionMultiplexer)
            ]);
        });

        return services;
    }
}

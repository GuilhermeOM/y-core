using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Y.Threads.Domain.Constants;
using Y.Threads.Infrastructure.Consumers.PostLike;

namespace Y.Threads.Infrastructure.Consumers;
internal static class KafkaTopologyConfiguration
{
    public static IServiceCollection ConfigureKafkaTopology(this IServiceCollection services, IConfiguration configuration)
    {
        var brokers = configuration.GetRequiredSection("Kafka:Brokers").Get<string[]>();

        services.AddKafka(kafka => kafka
            .UseMicrosoftLog()
            .AddCluster(cluster => cluster
                .WithBrokers(brokers)
                .ConfigureTopics()
                .ConfigureProducers()
                .ConfigureConsumers()
            )
        );

        return services;
    }

    public static IClusterConfigurationBuilder ConfigureTopics(this IClusterConfigurationBuilder cluster)
    {
        return cluster
            .CreateTopicIfNotExists(KafkaConstants.Topics.PostLikeTopic, 1, 1);
    }

    public static IClusterConfigurationBuilder ConfigureProducers(this IClusterConfigurationBuilder cluster)
    {
        return cluster
            .AddProducer(KafkaConstants.Producers.PostLikeProducer, producer => producer
                .DefaultTopic(KafkaConstants.Topics.PostLikeTopic)
                .AddMiddlewares(middlewares => middlewares.AddSerializer<JsonCoreSerializer>())
            );
    }

    public static IClusterConfigurationBuilder ConfigureConsumers(this IClusterConfigurationBuilder cluster)
    {
        return cluster
            .AddConsumer(consumer => consumer
                .Topic(KafkaConstants.Topics.PostLikeTopic)
                .WithGroupId(KafkaConstants.ConsumerGroups.ThreadsBaseConsumerGroup)
                .WithBufferSize(100)
                .WithWorkersCount(10)
                .AddMiddlewares(middlewares => middlewares.AddConsumerHandlers(
                [
                    typeof(PostLikeRequestConsumerHandler)
                ]))
            );
    }

    public static IConsumerMiddlewareConfigurationBuilder AddConsumerHandlers(
        this IConsumerMiddlewareConfigurationBuilder middleware,
        IEnumerable<Type> handlers)
    {
        return middleware
            .AddDeserializer<JsonCoreDeserializer>()
            .Add<LoggingConsumerMiddleware>()
            .AddTypedHandlers(handlerConfiguration => handlerConfiguration
                .WithHandlerLifetime(InstanceLifetime.Scoped)
                .AddHandlers(handlers));
    }
}

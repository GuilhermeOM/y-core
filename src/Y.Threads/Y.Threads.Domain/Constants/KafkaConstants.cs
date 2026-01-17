namespace Y.Threads.Domain.Constants;
public static class KafkaConstants
{
    public static class Producers
    {
        public const string Threads = "threads.default.producer";
    }

    public static class Topics
    {
        public const string PostLikeTopic = "threads.post-like.topic";
        public const string PostDislikeTopic = "threads.post-dislike.topic";
    }

    public static class ConsumerGroups
    {
        public const string ThreadsBaseConsumerGroup = "threads.base.consumergroup";
    }
}

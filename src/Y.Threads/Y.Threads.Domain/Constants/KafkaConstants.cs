namespace Y.Threads.Domain.Constants;
public static class KafkaConstants
{
    public static class Producers
    {
        public const string PostLikeProducer = "threads.posts-like.consumer";
    }

    public static class Topics
    {
        public const string PostLikeTopic = "threads.posts-like.topic";
    }

    public static class ConsumerGroups
    {
        public const string ThreadsBaseConsumerGroup = "threads.base.consumergroup";
    }
}

namespace Y.Threads.Domain.Constants;
public static class RedisConstants
{
    public static class Lock
    {
        public static string GetPostOperationLockName(Guid userId, Guid postId) => $"threads.{userId}-{postId}.lock";
    }
}

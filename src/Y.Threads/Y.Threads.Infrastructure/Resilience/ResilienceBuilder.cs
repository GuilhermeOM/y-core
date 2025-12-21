using Polly;
using Polly.Retry;

namespace Y.Threads.Infrastructure.Resilience;
internal static class ResilienceBuilder
{
    public static ResiliencePipelineBuilder FastDefaultRetryPipelinePolicy(ResiliencePipelineBuilder builder)
    {
        var retryStrategy = new RetryStrategyOptions()
        {
            ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is not null),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(3),
        };

        return builder.AddRetry(retryStrategy);
    }
}

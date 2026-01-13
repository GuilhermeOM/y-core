using KafkaFlow;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Y.Threads.Infrastructure.Consumers;

public class LoggingConsumerMiddleware : IMessageMiddleware
{
    private readonly ILogger<LoggingConsumerMiddleware> _logger;

    public LoggingConsumerMiddleware(ILogger<LoggingConsumerMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        using (LogContext.PushProperty("ConsumerName", context.ConsumerContext.ConsumerName))
        using (LogContext.PushProperty("MessageName", context.Message.GetType().Name))
        using (LogContext.PushProperty("MessageTimestamp", context.ConsumerContext.MessageTimestamp))
        using (LogContext.PushProperty("Topic", context.ConsumerContext.Topic))
        using (LogContext.PushProperty("Offset", context.ConsumerContext.Offset))
        using (LogContext.PushProperty("Partition", context.ConsumerContext.Partition))
        {
            try
            {
                _logger.LogInformation("Processing kafka consumer {ConsumerName}", context.ConsumerContext.ConsumerName);

                await next(context).ConfigureAwait(false);

                _logger.LogInformation("Completed kafka consumer {ConsumerName}", context.ConsumerContext.ConsumerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Completed kafka consumer {ConsumerName} with error", context.ConsumerContext.ConsumerName);
                throw;
            }
        }
    }
}

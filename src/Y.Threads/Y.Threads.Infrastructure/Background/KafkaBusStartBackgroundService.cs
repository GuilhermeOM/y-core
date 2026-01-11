using KafkaFlow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Y.Threads.Infrastructure.Background;
internal sealed class KafkaBusStartBackgroundService : IHostedService
{
    private readonly ILogger<KafkaBusStartBackgroundService> _logger;
    private readonly IKafkaBus _kafkaBus;

    public KafkaBusStartBackgroundService(
        ILogger<KafkaBusStartBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _kafkaBus = serviceProvider.CreateKafkaBus();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting kafka bus");

        await _kafkaBus.StartAsync(cancellationToken);

        _logger.LogInformation("Kafka bus successfully started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stoping kafka bus");

        await _kafkaBus.StopAsync();

        _logger.LogInformation("Kafka bus successfully stopped");
    }
}

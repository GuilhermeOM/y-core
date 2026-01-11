using KafkaFlow.Producers;
using Polly.Registry;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Services;
using Y.Threads.Infrastructure.Resilience;

namespace Y.Threads.Infrastructure.Services;
internal sealed class ProducerService : IProducerService
{
    private readonly IProducerAccessor _producerAccessor;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;

    public ProducerService(
        IProducerAccessor producerAccessor,
        ResiliencePipelineProvider<string> resiliencePipelineProvider)
    {
        _producerAccessor = producerAccessor;
        _resiliencePipelineProvider = resiliencePipelineProvider;
    }

    public async Task ProduceAsync(IKafkaMessage message, MessageMetadata metadata)
    {
        await _resiliencePipelineProvider
            .GetPipeline(Resiliences.FastDefaultRetryPipelinePolicy)
            .ExecuteAsync(async _ =>
            {
                await _producerAccessor[metadata.ProducerName].ProduceAsync(metadata.MessageKey, message);
            });
    }
}

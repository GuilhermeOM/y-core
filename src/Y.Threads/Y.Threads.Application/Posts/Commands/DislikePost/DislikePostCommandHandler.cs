using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Constants;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Services;

namespace Y.Threads.Application.Posts.Commands.DislikePost;
internal sealed class DislikePostCommandHandler : ICommandHandler<DislikePostCommand>
{
    private readonly IProducerService _producerService;

    public DislikePostCommandHandler(IProducerService producerService)
    {
        _producerService = producerService;
    }

    public async Task<Result> HandleAsync(DislikePostCommand command, CancellationToken cancellationToken = default)
    {
        var @event = new PostDislikeRequestEvent(command.PostId, command.UserId);

        await _producerService.ProduceAsync(@event, new()
        {
            MessageKey = @event.UserId.ToString(),
            Topic = KafkaConstants.Topics.PostDislikeTopic,
        });

        return Result.Success();
    }
}

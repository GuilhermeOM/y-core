using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Constants;
using Y.Threads.Domain.Events;
using Y.Threads.Domain.Services;

namespace Y.Threads.Application.Posts.Commands.LikePost;
internal sealed class LikePostCommandHandler : ICommandHandler<LikePostCommand>
{
    private readonly IProducerService _producerService;

    public LikePostCommandHandler(IProducerService producerService)
    {
        _producerService = producerService;
    }

    public async Task<Result> HandleAsync(LikePostCommand command, CancellationToken cancellationToken = default)
    {
        var @event = new PostLikeRequestEvent(command.PostId, command.UserId);

        await _producerService.ProduceAsync(@event, new()
        {
            MessageKey = @event.UserId.ToString(),
            Topic = KafkaConstants.Topics.PostLikeTopic,
        });

        return Result.Success();
    }
}

using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Application.Posts.Commands.DislikePost;
public sealed record DislikePostCommand : ICommand
{
    public required Guid PostId { get; set; }
    public Guid UserId { get; set; }
}

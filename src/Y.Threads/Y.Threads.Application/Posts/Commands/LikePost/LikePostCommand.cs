using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Application.Posts.Commands.LikePost;
public sealed class LikePostCommand : ICommand
{
    public required Guid PostId { get; set; }
    public Guid UserId { get; set; }
}

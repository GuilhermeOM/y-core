using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Application.Posts.Commands.LikePost;
public sealed record LikePostCommand : ICommand
{
    public required Guid PostId { get; set; }
    public Guid UserId { get; set; }
}

using Microsoft.AspNetCore.Http;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Application.Posts.Commands.ReplyPost;
public sealed record ReplyPostCommand : ICommand<Guid>
{
    public required Guid Parent { get; set; }
    public required Author Author { get; set; }
    public string Text { get; set; } = string.Empty;
    public IReadOnlyCollection<CreateMediaPost> Medias { get; set; } = [];
}

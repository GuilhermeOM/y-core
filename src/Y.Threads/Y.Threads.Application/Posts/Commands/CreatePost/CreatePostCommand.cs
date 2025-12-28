using Microsoft.AspNetCore.Http;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Core.SharedKernel.Models;

namespace Y.Threads.Application.Posts.Commands.CreatePost;
public sealed class CreatePostCommand : ICommand<Guid>
{
    public string Text { get; set; } = string.Empty;
    public ICollection<CreateMediaPost> Medias { get; set; } = [];
    public Guid Parent { get; set; } = Guid.Empty;
    public required Author Author { get; set; }
}

public sealed record CreateMediaPost(IFormFile Media, string Description = "");

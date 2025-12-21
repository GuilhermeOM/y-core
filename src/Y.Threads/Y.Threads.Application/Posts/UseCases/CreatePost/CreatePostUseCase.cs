using Microsoft.AspNetCore.Http;
using Y.Core.SharedKernel.Abstractions;
using Y.Threads.Domain.Entities;

namespace Y.Threads.Application.Posts.UseCases.CreatePost;
public sealed class CreatePostUseCase : IUseCase<Guid>
{
    public string Text { get; set; } = string.Empty;
    public ICollection<CreateMediaPost> Medias { get; set; } = [];
    public Guid Parent { get; set; } = Guid.Empty;
    public required Author Author { get; set; }
}

public sealed record CreateMediaPost(IFormFile Media, string Description = "");

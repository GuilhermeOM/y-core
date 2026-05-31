using Y.Core.SharedKernel.Models;

namespace Y.Threads.Application.Threads.Models;

public sealed record AuthorSnapshot
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string AvatarUrl { get; init; }

    public AuthorSnapshot(Author author)
    {
        Id = author.Id;
        Name = author.Name;
        AvatarUrl = author.AvatarUrl;
    }
}

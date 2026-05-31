using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Application.Threads.Models;

public sealed record MediaSnapshot
{
    public Guid MediaId { get; init; }
    public string Url { get; init; }
    public string Description { get; init; }
    public string Mime { get; init; }
    public MediaType Type { get; init; }

    public MediaSnapshot(Media media)
    {
        MediaId = media.Id;
        Url = media.Url;
        Description = media.Description;
        Mime = media.Mime;
        Type = Media.GetMediaTypeByMime(media.Mime);
    }
}

using Y.Core.SharedKernel;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Domain.Aggregates.Post;
public class Media : Entity
{
    private static readonly HashSet<string> _supportedMimeTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "video/mp4",
        "video/webm"
    ];

    public string Name { get; set; } = string.Empty;
    public string Mime { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    private Media()
    {
    }

    internal static Result<Media> Create(MediaUpload uploadMedia)
    {
        if (!IsSupportedMimeType(uploadMedia.Mime))
        {
            return Result.Failure<Media>(PostErrors.UnsupportedMediaType);
        }

        return Result.Success(new Media
        {
            Id = Guid.NewGuid(),
            Name = uploadMedia.Name,
            Mime = uploadMedia.Mime,
            Url = uploadMedia.Url,
            Description = uploadMedia.Description
        });
    }

    public static MediaType GetMediaTypeByMime(string mime)
    {
        var sanitizedMime = mime.Trim().ToUpperInvariant();

        return sanitizedMime.Split('/')[0] switch
        {
            "VIDEO" => MediaType.Video,
            "IMAGE" => MediaType.Image,
            _ => MediaType.Unknown
        };
    }

    public static bool IsSupportedMimeType(string contentType) => _supportedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
}

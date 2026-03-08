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

    public string Mime { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    private Media()
    {
    }

    internal static Result<Media> Create(FileUploadResult mediaUploadResult)
    {
        if (!IsSupportedMimeType(mediaUploadResult.Mime))
        {
            return Result.Failure<Media>(PostErrors.UnsupportedMediaType);
        }

        return Result.Success(new Media
        {
            Mime = mediaUploadResult.Mime,
            Url = mediaUploadResult.Url,
            Description = mediaUploadResult.Description
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

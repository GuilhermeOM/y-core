using Y.Threads.Domain.Models;

namespace Y.Threads.Domain.Constants;

public static class MediaConstants
{
    private static readonly string[] _imageContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    private static readonly string[] _videoContentTypes =
    [
        "video/mp4",
        "video/webm"
    ];

    public static MediaType GetMediaType(string contentType)
    {
        if (_imageContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return MediaType.Image;
        }

        if (_videoContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return MediaType.Video;
        }

        return MediaType.Unknown;
    }

    public static string[] GetAllowedMimes() => [.. _imageContentTypes.Union(_videoContentTypes)];

    public static bool IsSupportedMimeType(string contentType) => GetAllowedMimes().Contains(contentType, StringComparer.OrdinalIgnoreCase);
}

using Y.Threads.Domain.Models;

namespace Y.Threads.Domain.Constants;
public static class MediaConstants
{
    private static readonly string[] _imageMimeTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    private static readonly string[] _videoMimeTypes =
    [
        "video/mp4",
        "video/webm"
    ];

    public static MediaType GetMediaType(string contentType)
    {
        if (_imageMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return MediaType.Image;
        }

        if (_videoMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return MediaType.Video;
        }

        return MediaType.Unknown;
    }

    public static string[] GetAllowedMimes() => [.. _imageMimeTypes.Union(_videoMimeTypes)];

    public static bool IsSupportedMimeType(string contentType) => GetAllowedMimes().Contains(contentType, StringComparer.OrdinalIgnoreCase);
}

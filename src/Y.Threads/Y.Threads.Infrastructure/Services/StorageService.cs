using MimeDetective;
using Y.Threads.Domain.Constants;
using Y.Threads.Domain.Models;
using Y.Threads.Domain.Services;

namespace Y.Threads.Infrastructure.Services;
internal sealed class StorageService : IStorageService
{
    private const string ImageBucket = "media/images";
    private const string VideoBucket = "media/videos";

    private readonly Supabase.Client _client;
    private readonly IContentInspector _contentInspector;

    public StorageService(Supabase.Client client, IContentInspector contentInspector)
    {
        _client = client;
        _contentInspector = contentInspector;
    }

    public async Task<Media?> UploadMediaAsync(Guid userId, Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        var memoryStreamArray = memoryStream.ToArray();

        var (Mime, Extension) = InspectFile(memoryStreamArray);

        if (string.IsNullOrEmpty(Mime) || string.IsNullOrEmpty(Extension))
        {
            return null;
        }

        return MediaConstants.GetMediaType(Mime) switch
        {
            MediaType.Image => await UploadImageAsync(userId, memoryStreamArray, Mime, Extension),
            MediaType.Video => await UploadVideoAsync(userId, memoryStreamArray, Mime, Extension),
            _ => null
        };
    }

    private (string? Mime, string? Extension) InspectFile(byte[] data)
    {
        var inspect = _contentInspector.Inspect(data);

        var mimeResults = inspect.ByMimeType();
        var extensionResults = inspect.ByFileExtension();

        var extension = extensionResults.FirstOrDefault()?.Extension;
        var mime = mimeResults.FirstOrDefault()?.MimeType;

        return new(mime, extension);
    }

    private async Task<Media> UploadImageAsync(
        Guid userId,
        byte[] data,
        string mime,
        string extension)
    {
        var mediaId = Guid.NewGuid();
        var mediaPath = CreateMediaPath(userId, mediaId.ToString(), extension);

        await _client.Storage
            .From(ImageBucket)
            .Upload(data, mediaPath, new Supabase.Storage.FileOptions { ContentType = mime });

        return new()
        {
            Id = mediaId,
            Url = _client.Storage.From(ImageBucket).GetPublicUrl(mediaPath),
            Type = MediaType.Image
        };
    }

    private async Task<Media> UploadVideoAsync(
        Guid userId,
        byte[] data,
        string mime,
        string extension)
    {
        var mediaId = Guid.NewGuid();
        var mediaPath = CreateMediaPath(userId, mediaId.ToString(), extension);

        await _client.Storage
            .From(VideoBucket)
            .Upload(data, mediaPath, new Supabase.Storage.FileOptions { ContentType = mime });

        return new()
        {
            Id = mediaId,
            Url = _client.Storage.From(VideoBucket).GetPublicUrl(mediaPath),
            Type = MediaType.Video
        };
    }

    private static string CreateMediaPath(Guid userId, string mediaId, string extension) => $"{userId:N}/{mediaId}.{extension}";
}

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

    public StorageService(Supabase.Client client)
    {
        _client = client;
    }

    public async Task<Media?> UploadMediaAsync(Guid userId, Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        var memoryStreamArray = memoryStream.ToArray();

        var inspection = InspectFile(memoryStreamArray);

        if (string.IsNullOrEmpty(inspection.Mime) || string.IsNullOrEmpty(inspection.Extension))
        {
            return null;
        }

        return MediaConstants.GetMediaType(inspection.Mime) switch
        {
            MediaType.Image => await UploadImageAsync(userId, memoryStream.ToArray(), inspection),
            MediaType.Video => await UploadVideoAsync(userId, memoryStream.ToArray(), inspection),
            _ => null
        };
    }

    private static FileInspection InspectFile(byte[] data)
    {
        var inspector = new ContentInspectorBuilder()
        {
            Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
        }.Build();

        var inspectResults = inspector.Inspect(data);
        var mimeResults = inspectResults.ByMimeType();
        var extensionResults = inspectResults.ByFileExtension();

        var extension = extensionResults.FirstOrDefault()?.Extension;
        var mime = mimeResults.FirstOrDefault()?.MimeType;

        return new(mime, extension);
    }

    private async Task<Media> UploadImageAsync(Guid userId, byte[] data, FileInspection inspection)
    {
        var mediaId = Guid.NewGuid();
        var mediaPath = CreateMediaPath(userId, mediaId.ToString(), inspection.Extension!);

        await _client.Storage
            .From(ImageBucket)
            .Upload(data, mediaPath, new Supabase.Storage.FileOptions { ContentType = inspection.Mime! });

        return new()
        {
            Id = mediaId,
            Url = _client.Storage.From(ImageBucket).GetPublicUrl(mediaPath),
            Type = MediaType.Image
        };
    }

    private async Task<Media> UploadVideoAsync(Guid userId, byte[] data, FileInspection inspection)
    {
        var mediaId = Guid.NewGuid();
        var mediaPath = CreateMediaPath(userId, mediaId.ToString(), inspection.Extension!);

        await _client.Storage
            .From(VideoBucket)
            .Upload(data, mediaPath, new Supabase.Storage.FileOptions { ContentType = inspection.Mime! });

        return new()
        {
            Id = mediaId,
            Url = _client.Storage.From(VideoBucket).GetPublicUrl(mediaPath),
            Type = MediaType.Video
        };
    }

    private static string CreateMediaPath(Guid userId, string mediaId, string extension) => $"{userId:N}/{mediaId}.{extension}";
}

public sealed record FileInspection(string? Mime, string? Extension);

using MimeDetective;
using Polly.Registry;
using Y.Threads.Domain.Constants;
using Y.Threads.Domain.Models;
using Y.Threads.Domain.Services;
using Y.Threads.Infrastructure.Resilience;

namespace Y.Threads.Infrastructure.Services;
internal sealed class StorageService : IStorageService
{
    private const string ImageBucket = "media/images";
    private const string VideoBucket = "media/videos";

    private readonly Supabase.Client _client;
    private readonly IContentInspector _contentInspector;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;

    public StorageService(
        Supabase.Client client,
        IContentInspector contentInspector,
        ResiliencePipelineProvider<string> resiliencePipelineProvider)
    {
        _client = client;
        _contentInspector = contentInspector;
        _resiliencePipelineProvider = resiliencePipelineProvider;
    }

    public async Task<Media?> UploadMediaAsync(Guid userId, Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        stream.Seek(0, SeekOrigin.Begin);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var memoryStreamArray = memoryStream.ToArray();

        var (Mime, Extension) = InspectFile(memoryStreamArray);

        if (string.IsNullOrEmpty(Mime) || string.IsNullOrEmpty(Extension))
        {
            return null;
        }

        return await _resiliencePipelineProvider
            .GetPipeline(Resiliences.FastDefaultRetryPipelinePolicy)
            .ExecuteAsync(async _ =>
        {
            return MediaConstants.GetMediaType(Mime) switch
            {
                MediaType.Image => await UploadImageAsync(userId, memoryStreamArray, Mime, Extension, cancellationToken),
                MediaType.Video => await UploadVideoAsync(userId, memoryStreamArray, Mime, Extension, cancellationToken),
                _ => null
            };
        }, cancellationToken);
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
        string extension,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mediaId = Guid.NewGuid();
        var mediaPath = CreateMediaPath(userId, mediaId, extension);

        await _client.Storage
            .From(ImageBucket)
            .Upload(data, mediaPath, new Supabase.Storage.FileOptions { ContentType = mime });

        var url = _client.Storage.From(ImageBucket).GetPublicUrl(mediaPath);

        return new()
        {
            Id = mediaId,
            Url = url,
            Mime = mime,
            Extension = extension,
            Type = MediaType.Image
        };
    }

    private async Task<Media> UploadVideoAsync(
        Guid userId,
        byte[] data,
        string mime,
        string extension,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mediaId = Guid.NewGuid();
        var mediaPath = CreateMediaPath(userId, mediaId, extension);

        await _client.Storage
            .From(VideoBucket)
            .Upload(data, mediaPath, new Supabase.Storage.FileOptions { ContentType = mime });

        var url = _client.Storage.From(ImageBucket).GetPublicUrl(mediaPath);

        return new()
        {
            Id = mediaId,
            Url = url,
            Mime = mime,
            Extension = extension,
            Type = MediaType.Video
        };
    }

    public async Task DeleteMediaAsync(Guid userId, Media media)
    {
        var bucket = media.Type switch
        {
            MediaType.Image => ImageBucket,
            MediaType.Video => VideoBucket,
            _ => throw new ArgumentOutOfRangeException(nameof(media), "Unsupported media type")
        };

        await _client.Storage
            .From(bucket)
            .Remove(CreateMediaPath(userId, media.Id, media.Extension));
    }

    private static string CreateMediaPath(Guid userId, Guid mediaId, string extension) => $"{userId:N}/{mediaId:N}.{extension}";
}

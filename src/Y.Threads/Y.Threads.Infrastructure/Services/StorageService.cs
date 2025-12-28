using MimeDetective;
using MongoDB.Driver;
using Polly.Registry;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;
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

    public async Task<MediaUpload?> UploadMediaAsync(Guid userId, Stream stream, CancellationToken cancellationToken = default)
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
                return await UploadAsync(
                    userId,
                    memoryStreamArray,
                    Mime,
                    Extension,
                    cancellationToken);
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

    private async Task<MediaUpload?> UploadAsync(
        Guid userId,
        byte[] data,
        string mime,
        string extension,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mediaName = CreateMediaName(extension);
        var mediaPath = CreateMediaPath(userId, mediaName);
        var mediaType = Media.GetMediaTypeByMime(mime);

        var bucket = mediaType switch
        {
            MediaType.Image => ImageBucket,
            MediaType.Video => VideoBucket,
            _ => null
        };

        if (bucket is null) return null;

        await _client.Storage
            .From(bucket)
            .Upload(data, mediaPath, new Supabase.Storage.FileOptions { ContentType = mime });

        var url = _client.Storage.From(ImageBucket).GetPublicUrl(mediaPath);

        return new(mediaName, url, mime);
    }

    public async Task DeleteMediaAsync(Guid userId, MediaUpload mediaUpload)
    {
        var bucket = Media.GetMediaTypeByMime(mediaUpload.Mime) switch
        {
            MediaType.Image => ImageBucket,
            MediaType.Video => VideoBucket,
            _ => throw new ArgumentOutOfRangeException(nameof(mediaUpload), "Unsupported media type")
        };

        await _client.Storage
            .From(bucket)
            .Remove(CreateMediaPath(userId, mediaUpload.Name));
    }

    private static string CreateMediaName(string extension) => $"{Guid.NewGuid():N}.{extension}";
    private static string CreateMediaPath(Guid userId, string mediaName) => $"{userId:N}/{mediaName}";
}

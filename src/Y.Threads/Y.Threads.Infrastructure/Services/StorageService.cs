using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Polly.Registry;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Options;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;
using Y.Threads.Infrastructure.Resilience;

namespace Y.Threads.Infrastructure.Services;
internal sealed class StorageService : IStorageService
{
    public const string PublicThreadsContainerName = "public-threads";

    private const string ImagePathName = "images";
    private const string VideoPathname = "videos";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;
    private readonly IFileInspectorService _fileInspectorService;
    private readonly IOptions<BlobStorageOptions> _blobStorageOptions;

    public StorageService(
        BlobServiceClient blobServiceClient,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        IFileInspectorService fileInspectorService,
        IOptions<BlobStorageOptions> blobStorageOptions)
    {
        _blobServiceClient = blobServiceClient;
        _resiliencePipelineProvider = resiliencePipelineProvider;
        _fileInspectorService = fileInspectorService;
        _blobStorageOptions = blobStorageOptions;
    }

    public async Task<MediaUpload?> UploadMediaAsync(
        Guid userId,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var inspectionResult = _fileInspectorService.InspectFileStream(stream);
        if (inspectionResult.IsFailure)
        {
            return null;
        }

        return await _resiliencePipelineProvider
            .GetPipeline(Resiliences.FastDefaultRetryPipelinePolicy)
            .ExecuteAsync(async _ =>
            {
                return await UploadAsync(
                    userId,
                    stream,
                    inspectionResult.Value.Mime,
                    inspectionResult.Value.Extension,
                    cancellationToken);
            }, cancellationToken);
    }

    private async Task<MediaUpload?> UploadAsync(
        Guid userId,
        Stream data,
        string mime,
        string extension,
        CancellationToken cancellationToken)
    {
        data.Seek(0, SeekOrigin.Begin);

        var mediaName = CreateMediaName(extension);
        var mediaPath = CreateMediaPath(mime, userId, mediaName);

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(PublicThreadsContainerName);
        var blobClient = blobContainerClient.GetBlobClient(mediaPath);

        var upload = await blobClient.UploadAsync(data, new BlobHttpHeaders
        {
            ContentType = mime,
            CacheControl = "public, max-age=31536000"
        }, cancellationToken: cancellationToken);

        if (upload.GetRawResponse().Status >= 400)
        {
            return null;
        }

        return new(mediaName, CreateMediaPublicUrl(mediaPath), mime);
    }

    public async Task DeleteMediaAsync(Guid userId, MediaUpload mediaUpload)
    {
        var mediaPath = CreateMediaPath(mediaUpload.Mime, userId, mediaUpload.Name);

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(PublicThreadsContainerName);
        var blobClient = blobContainerClient.GetBlobClient(mediaPath);

        await blobClient.DeleteIfExistsAsync();
    }

    private static string CreateMediaName(string extension) => $"{Guid.NewGuid():N}.{extension}";

    private static string CreateMediaPath(string mime, Guid userId, string mediaName)
    {
        var rootPath = Media.GetMediaTypeByMime(mime) switch
        {
            MediaType.Image => ImagePathName,
            MediaType.Video => VideoPathname,
            _ => throw new ArgumentOutOfRangeException(nameof(mime), "Unsupported media type")
        };

        return $"{rootPath}/{userId:N}/{mediaName}";
    }

    private string CreateMediaPublicUrl(string mediaPath) => $"{_blobStorageOptions.Value.BaseUrl}/{PublicThreadsContainerName}/{mediaPath}";
}

using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Polly.Registry;
using Y.Core.SharedKernel;
using Y.Threads.Domain.Options;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;
using Y.Threads.Infrastructure.Resilience;

namespace Y.Threads.Infrastructure.Services;
internal sealed class StorageService : IStorageService
{
    public const string PublicThreadsContainerName = "public-threads";

    public const string ImagePathName = "images";
    public const string VideoPathname = "videos";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;
    private readonly IOptions<BlobStorageOptions> _blobStorageOptions;

    public StorageService(
        BlobServiceClient blobServiceClient,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        IOptions<BlobStorageOptions> blobStorageOptions)
    {
        _blobServiceClient = blobServiceClient;
        _resiliencePipelineProvider = resiliencePipelineProvider;
        _blobStorageOptions = blobStorageOptions;
    }

    public async Task<Result<FileUploadResult>> UploadAsync(
        FileUpload fileUpload,
        CancellationToken cancellationToken = default)
    {
        return await _resiliencePipelineProvider
            .GetPipeline(Resiliences.FastDefaultRetryPipelinePolicy)
            .ExecuteAsync(async _ =>
            {
                return await UploadMediaAsync(fileUpload,cancellationToken);
            }, cancellationToken);
    }

    private async Task<Result<FileUploadResult>> UploadMediaAsync(
        FileUpload fileUpload,
        CancellationToken cancellationToken)
    {
        fileUpload.Data.Seek(0, SeekOrigin.Begin);

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(PublicThreadsContainerName);
        var blobClient = blobContainerClient.GetBlobClient(fileUpload.Path);

        var upload = await blobClient.UploadAsync(fileUpload.Data, new BlobHttpHeaders
        {
            ContentType = fileUpload.Mime,
            CacheControl = "public, max-age=31536000"
        }, cancellationToken: cancellationToken);

        if (upload.GetRawResponse().Status >= (int)HttpStatusCode.BadRequest)
        {
            return Result.Failure<FileUploadResult>(StorageServiceErrors.BlobStorageFailure);
        }

        var uploadResult = new FileUploadResult(
            fileUpload.BlobId,
            CreateFilePublicUrl(fileUpload.Path),
            fileUpload.Path,
            fileUpload.Mime,
            fileUpload.Description);

        return Result.Success(uploadResult);
    }

    public async Task DeleteAsync(string filePath)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(PublicThreadsContainerName);
        var blobClient = blobContainerClient.GetBlobClient(filePath);

        await blobClient.DeleteIfExistsAsync();
    }

    private string CreateFilePublicUrl(string filePath) => $"{_blobStorageOptions.Value.BaseUrl}/{PublicThreadsContainerName}/{filePath}";
}

internal static class StorageServiceErrors
{
    public static Error BlobStorageFailure => new("BLOB_STORAGE_FAILURE", "An error occurred while communicating with blob storage");
}

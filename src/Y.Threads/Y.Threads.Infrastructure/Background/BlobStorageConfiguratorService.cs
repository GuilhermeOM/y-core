using Azure.Storage.Blobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Y.Threads.Infrastructure.Services;

namespace Y.Threads.Infrastructure.Background;
internal sealed class BlobStorageConfiguratorService : IHostedService
{
    private readonly ILogger<BlobStorageConfiguratorService> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageConfiguratorService(
        ILogger<BlobStorageConfiguratorService> logger,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting blob storage configuraton");

        var containers = _blobServiceClient.GetBlobContainers(cancellationToken: cancellationToken);
        var containerNames = containers.Select(container => container.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (containerNames.Contains(StorageService.PublicThreadsContainerName, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Blob storage configuration successfully completed");
            return;
        }

        var createResult = await _blobServiceClient
            .CreateBlobContainerAsync(StorageService.PublicThreadsContainerName, cancellationToken: cancellationToken);

        if (createResult.GetRawResponse().Status != 201)
        {
            _logger.LogError("Failed to create container {ContainerName}. Status code: {StatusCode}", StorageService.PublicThreadsContainerName, createResult.GetRawResponse().Status);
            return;
        }

        var policySetupResult = _blobServiceClient
            .GetBlobContainerClient(StorageService.PublicThreadsContainerName)
            .SetAccessPolicy(accessType: Azure.Storage.Blobs.Models.PublicAccessType.Blob, cancellationToken: cancellationToken);

        if (policySetupResult.GetRawResponse().Status != 200)
        {
            _logger.LogError("Failed to setup policy for {ContainerName}. Status code: {StatusCode}", StorageService.PublicThreadsContainerName, policySetupResult.GetRawResponse().Status);
            return;
        }

        _logger.LogInformation("Blob storage configuration successfully completed");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

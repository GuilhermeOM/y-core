using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Registry;
using Y.Core.SharedKernel;
using Y.Threads.Domain.Options;
using Y.Threads.Domain.Services;
using Y.Threads.Infrastructure.Resilience;
using Y.Threads.Infrastructure.Services;

namespace Y.Core.UnitTest.Y.Threads.Services;
public class StorageServiceTests
{
    private readonly Mock<BlobServiceClient> _blobServiceClientMock;
    private readonly Mock<ResiliencePipelineProvider<string>> _resiliencePipelineProviderMock;
    private readonly Mock<IFileInspectorService> _fileInspectorServiceMock;
    private readonly Mock<IOptions<BlobStorageOptions>> _blobStorageOptionsMock;
    private readonly Mock<BlobContainerClient> _blobContainerClientMock;
    private readonly Mock<BlobClient> _blobClientMock;

    private readonly StorageService _service;

    public StorageServiceTests()
    {
        _blobServiceClientMock = new Mock<BlobServiceClient>();
        _resiliencePipelineProviderMock = new Mock<ResiliencePipelineProvider<string>>();
        _fileInspectorServiceMock = new Mock<IFileInspectorService>();
        _blobStorageOptionsMock = new Mock<IOptions<BlobStorageOptions>>();
        _blobContainerClientMock = new Mock<BlobContainerClient>();
        _blobClientMock = new Mock<BlobClient>();

        _resiliencePipelineProviderMock
           .Setup(mock => mock.GetPipeline(It.Is<string>(x => x == Resiliences.FastDefaultRetryPipelinePolicy)))
           .Returns(ResiliencePipeline.Empty);

        _blobStorageOptionsMock
            .SetupGet(mock => mock.Value)
            .Returns(new BlobStorageOptions
            {
                BaseUrl = "http://localhost:10000"
            });

        _blobServiceClientMock
            .Setup(client => client.GetBlobContainerClient(StorageService.PublicThreadsContainerName))
            .Returns(_blobContainerClientMock.Object);

        _service = new StorageService(
            _blobServiceClientMock.Object,
            _resiliencePipelineProviderMock.Object,
            _fileInspectorServiceMock.Object,
            _blobStorageOptionsMock.Object);
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnNull_WhenFileInspectionFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stream = new MemoryStream([0x00, 0x01, 0x02]);

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(stream))
            .Returns(Result.Failure<(string Mime, string Extension)>(new Error("Inspection.Failed", "File inspection failed")));

        // Act
        var result = await _service.UploadAsync(userId, stream);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnNull_WhenMediaUploadFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stream = new MemoryStream([0x00, 0x01, 0x02]);
        const string mime = "image/jpeg";
        const string extension = "jpg";

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(stream))
            .Returns(Result.Success((mime, extension)));

        _blobContainerClientMock
            .Setup(mock => mock.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        var responseMock = new Mock<Response<BlobContentInfo>>();
        responseMock.SetupGet(mock => mock.GetRawResponse().Status).Returns(500);

        _blobClientMock
            .Setup(client => client.UploadAsync(
                stream,
                It.Is<BlobHttpHeaders>(headers => headers.ContentType == mime && headers.CacheControl == "public, max-age=31536000"),
                default,
                default,
                default,
                default,
                default,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock.Object);

        // Act
        var result = await _service.UploadAsync(userId, stream);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UploadAsync_ShouldSucceed_WhenUploadingImage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stream = new MemoryStream([0x00, 0x01, 0x02]);
        const string mime = "image/jpeg";
        const string extension = "jpg";

        _fileInspectorServiceMock
            .Setup(service => service.InspectFileStream(stream))
            .Returns(Result.Success((mime, extension)));

        _blobContainerClientMock
            .Setup(client => client.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        var responseMock = new Mock<Response<BlobContentInfo>>();
        responseMock.SetupGet(mock => mock.GetRawResponse().Status).Returns(201);

        _blobClientMock
            .Setup(client => client.UploadAsync(
                stream,
                It.Is<BlobHttpHeaders>(headers => headers.ContentType == mime && headers.CacheControl == "public, max-age=31536000"),
                default,
                default,
                default,
                default,
                default,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock.Object);

        // Act
        var result = await _service.UploadAsync(userId, stream);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().NotBeNullOrEmpty();
        result.Name.Should().EndWith($".{extension}");
        result.Mime.Should().Be(mime);
        result.Url.Should().NotBeNullOrEmpty();
        result.Url.Should().StartWith($"{_blobStorageOptionsMock.Object.Value.BaseUrl}/{StorageService.PublicThreadsContainerName}/{StorageService.ImagePathName}/");
        result.Url.Should().Contain(userId.ToString("N"));
    }

    [Fact]
    public async Task UploadAsync_ShouldSucceed_WhenUploadingVideo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stream = new MemoryStream([0x00, 0x01, 0x02]);
        const string mime = "video/mp4";
        const string extension = "mp4";

        _fileInspectorServiceMock
            .Setup(service => service.InspectFileStream(stream))
            .Returns(Result.Success((mime, extension)));

        _blobContainerClientMock
            .Setup(client => client.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        var responseMock = new Mock<Response<BlobContentInfo>>();
        responseMock.SetupGet(mock => mock.GetRawResponse().Status).Returns(201);

        _blobClientMock
            .Setup(client => client.UploadAsync(
                stream,
                It.Is<BlobHttpHeaders>(headers => headers.ContentType == mime && headers.CacheControl == "public, max-age=31536000"),
                default,
                default,
                default,
                default,
                default,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock.Object);

        // Act
        var result = await _service.UploadAsync(userId, stream);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().NotBeNullOrEmpty();
        result.Name.Should().EndWith($".{extension}");
        result.Mime.Should().Be(mime);
        result.Url.Should().NotBeNullOrEmpty();
        result.Url.Should().StartWith($"{_blobStorageOptionsMock.Object.Value.BaseUrl}/{StorageService.PublicThreadsContainerName}/{StorageService.VideoPathname}/");
        result.Url.Should().Contain(userId.ToString("N"));
    }
}

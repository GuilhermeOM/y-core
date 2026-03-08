using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Y.Core.SharedKernel;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Application.Posts.Services.CreatePostMedia;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;

namespace Y.Core.UnitTest.Y.Threads.Posts.Services;
public class CreatePostMediaServiceTests
{
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IFileInspectorService> _fileInspectorServiceMock;

    private readonly Mock<IFormFile> _file0Mock;
    private readonly Mock<IFormFile> _file1Mock;

    private readonly CreatePostMediaService _service;

    public CreatePostMediaServiceTests()
    {
        _storageServiceMock = new Mock<IStorageService>();
        _fileInspectorServiceMock = new Mock<IFileInspectorService>();

        _file0Mock = new Mock<IFormFile>();
        _file1Mock = new Mock<IFormFile>();

        var file0Data = "file0";
        var file0ByteArray = Encoding.UTF8.GetBytes(file0Data);
        var file0Stream = new MemoryStream(file0ByteArray);

        var file1Data = "file1";
        var file1ByteArray = Encoding.UTF8.GetBytes(file1Data);
        var file1Stream = new MemoryStream(file1ByteArray);

        _file0Mock.Setup(mock => mock.OpenReadStream()).Returns(file0Stream);
        _file1Mock.Setup(mock => mock.OpenReadStream()).Returns(file1Stream);

        _service = new CreatePostMediaService(
            _storageServiceMock.Object,
            _fileInspectorServiceMock.Object);
    }

    [Fact]
    public async Task UploadManyAsync_ShouldFail_WhenFileInspectionResultFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medias = new List<CreateMediaPost>()
        {
            new(_file0Mock.Object),
            new(_file1Mock.Object)
        };

        var expectedError = new Error("INSPECTION_FAILURE", "File inspection failed");

        using var file0Stream = _file0Mock.Object.OpenReadStream();
        using var file1Stream = _file1Mock.Object.OpenReadStream();

        var file0UploadResult = new FileUploadResult(
            Guid.NewGuid(),
            string.Empty,
            "file1Path",
            string.Empty,
            string.Empty);

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file0Stream))
            .Returns(Result.Success(new FileInspectionResult("image/jpeg", "jpg")));

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file1Stream))
            .Returns(Result.Failure<FileInspectionResult>(expectedError));

        _storageServiceMock
            .Setup(mock => mock.UploadAsync(
                It.Is<FileUpload>(fu => fu.Data == file0Stream), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(file0UploadResult));

        // Act
        var result = await _service.UploadManyAsync(userId, medias, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(expectedError);

        _storageServiceMock.Verify(mock => mock.UploadAsync(
            It.Is<FileUpload>(fu => fu.Data == file1Stream), It.IsAny<CancellationToken>()), Times.Never);

        _storageServiceMock.Verify(mock => mock.DeleteAsync(file0UploadResult.Path), Times.Once);
    }

    [Fact]
    public async Task UploadManyAsync_ShouldFail_WhenMimeTypeIsNotSupported()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medias = new List<CreateMediaPost>()
        {
            new(_file0Mock.Object),
            new(_file1Mock.Object)
        };

        using var file0Stream = _file0Mock.Object.OpenReadStream();
        using var file1Stream = _file1Mock.Object.OpenReadStream();

        var file0UploadResult = new FileUploadResult(
            Guid.NewGuid(),
            string.Empty,
            "file1Path",
            string.Empty,
            string.Empty);

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file0Stream))
            .Returns(Result.Success(new FileInspectionResult("image/jpeg", "jpg")));

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file1Stream))
            .Returns(Result.Success(new FileInspectionResult("dummy/type", "dummy")));

        _storageServiceMock
            .Setup(mock => mock.UploadAsync(
                It.Is<FileUpload>(fu => fu.Data == file0Stream), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(file0UploadResult));

        // Act
        var result = await _service.UploadManyAsync(userId, medias, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.UnsupportedMediaType);

        _storageServiceMock.Verify(mock => mock.UploadAsync(
            It.Is<FileUpload>(fu => fu.Data == file1Stream), It.IsAny<CancellationToken>()), Times.Never);

        _storageServiceMock.Verify(mock => mock.DeleteAsync(file0UploadResult.Path), Times.Once);
    }

    [Fact]
    public async Task UploadManyAsync_ShouldFail_WhenExceptionsIsThrown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medias = new List<CreateMediaPost>()
        {
            new(_file0Mock.Object),
            new(_file1Mock.Object)
        };

        using var file0Stream = _file0Mock.Object.OpenReadStream();
        using var file1Stream = _file1Mock.Object.OpenReadStream();

        var file0UploadResult = new FileUploadResult(
            Guid.NewGuid(),
            string.Empty,
            "file1Path",
            string.Empty,
            string.Empty);

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file0Stream))
            .Returns(Result.Success(new FileInspectionResult("image/jpeg", "jpg")));

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file1Stream))
            .Returns(Result.Success(new FileInspectionResult("image/jpeg", "jpg")));

        _storageServiceMock
            .Setup(mock => mock.UploadAsync(
                It.Is<FileUpload>(fu => fu.Data == file0Stream), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(file0UploadResult));

        _storageServiceMock
            .Setup(mock => mock.UploadAsync(
                It.Is<FileUpload>(fu => fu.Data == file1Stream), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _service.UploadManyAsync(userId, medias, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(PostErrors.MediaUploadFailed);

        _storageServiceMock.Verify(mock => mock.DeleteAsync(file0UploadResult.Path), Times.Once);
    }

    [Fact]
    public async Task UploadManyAsync_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medias = new List<CreateMediaPost>()
        {
            new(_file0Mock.Object),
            new(_file1Mock.Object)
        };

        using var file0Stream = _file0Mock.Object.OpenReadStream();
        using var file1Stream = _file1Mock.Object.OpenReadStream();

        var file0UploadResult = new FileUploadResult(
            Guid.NewGuid(),
            string.Empty,
            "file0Path",
            string.Empty,
            string.Empty);

        var file1UploadResult = new FileUploadResult(
            Guid.NewGuid(),
            string.Empty,
            "file1Path",
            string.Empty,
            string.Empty);

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file0Stream))
            .Returns(Result.Success(new FileInspectionResult("image/jpeg", "jpg")));

        _fileInspectorServiceMock
            .Setup(mock => mock.InspectFileStream(file1Stream))
            .Returns(Result.Success(new FileInspectionResult("image/jpeg", "jpg")));

        _storageServiceMock
            .Setup(mock => mock.UploadAsync(
                It.Is<FileUpload>(fu => fu.Data == file0Stream), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(file0UploadResult));

        _storageServiceMock
            .Setup(mock => mock.UploadAsync(
                It.Is<FileUpload>(fu => fu.Data == file1Stream), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(file1UploadResult));

        // Act
        var result = await _service.UploadManyAsync(userId, medias, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Length.Should().Be(medias.Count);
        result.Value[0].BlobId.Should().Be(file0UploadResult.BlobId);
        result.Value[1].BlobId.Should().Be(file1UploadResult.BlobId);
        result.Value[0].Path.Should().Be(file0UploadResult.Path);
        result.Value[1].Path.Should().Be(file1UploadResult.Path);

        _storageServiceMock.Verify(mock => mock.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}

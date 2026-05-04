using Y.Core.SharedKernel;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Application.Posts.Services.CreatePostMedia;
internal sealed class CreatePostMediaService : ICreatePostMediaService
{
    private const string ImagePathName = "images";
    private const string VideoPathname = "videos";

    private readonly IStorageService _storageService;
    private readonly IFileInspectorService _fileInspectorService;

    public CreatePostMediaService(
        IStorageService storageService,
        IFileInspectorService fileInspectorService)
    {
        _storageService = storageService;
        _fileInspectorService = fileInspectorService;
    }

    public async Task<Result<FileUploadResult[]>> UploadManyAsync(
        Guid userId,
        IReadOnlyCollection<CreateMediaPost> medias,
        CancellationToken cancellationToken = default)
    {
        var uploadTaskPool = new List<Task<Result<FileUploadResult>>>();

        foreach (var media in medias)
        {
            var uploadTask = UploadMediaAsync(userId, media, cancellationToken);
            uploadTaskPool.Add(uploadTask);
        }

        var uploadTasksResult = await Task.WhenAll(uploadTaskPool);

        var uploadedMedias = uploadTasksResult
            .Where(result => result.IsSuccess)
            .Select(result => result.Value)
            .ToArray();

        var failedUpload = uploadTasksResult.FirstOrDefault(result => result.IsFailure);
        if (failedUpload is not null)
        {
            await RollbackAsync(uploadedMedias);
            return Result.Failure<FileUploadResult[]>(failedUpload.Error);
        }

        return Result.Success(uploadedMedias);
    }

    private async Task<Result<FileUploadResult>> UploadMediaAsync(
        Guid userId,
        CreateMediaPost file,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = file.Media.OpenReadStream();

            var inspectionResult = _fileInspectorService.InspectFileStream(stream);
            if (inspectionResult.IsFailure)
            {
                return Result.Failure<FileUploadResult>(inspectionResult.Error);
            }

            if (!Media.IsSupportedMimeType(inspectionResult.Value.Mime))
            {
                return Result.Failure<FileUploadResult>(PostErrors.UnsupportedMediaType);
            }

            var blobId = Guid.NewGuid();

            var mediaPath = CreateMediaPath(
                userId,
                inspectionResult.Value,
                blobId.ToString("N"));

            var fileUpload = new FileUpload(
                blobId,
                stream,
                mediaPath,
                inspectionResult.Value.Mime,
                inspectionResult.Value.Extension,
                file.Description);

            return await _storageService.UploadAsync(fileUpload, cancellationToken);
        }
        catch
        {
            return Result.Failure<FileUploadResult>(PostErrors.MediaUploadFailed);
        }
    }

    private static string CreateMediaPath(Guid userId, FileInspectionResult fileInspectionResult, string mediaName)
    {
        var rootPath = Media.GetMediaTypeByMime(fileInspectionResult.Mime) switch
        {
            MediaType.Image => ImagePathName,
            MediaType.Video => VideoPathname,
            _ => throw new ArgumentOutOfRangeException(nameof(fileInspectionResult), "Unsupported media type")
        };

        return $"{rootPath}/{userId:N}/{mediaName}.{fileInspectionResult.Extension}";
    }

    public async Task RollbackAsync(IReadOnlyCollection<FileUploadResult> medias)
    {
        medias ??= [];

        var deleteMediaTasks = medias.Select(media => _storageService.DeleteAsync(media.Path));
        await Task.WhenAll(deleteMediaTasks);
    }
}

using System.Collections.Concurrent;
using Y.Threads.Domain.Services;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Application.Posts.Commands.CreatePost;
internal sealed class CreatePostMediaUploadManager
{
    private readonly ConcurrentBag<MediaUpload> _uploadedMedias = [];

    private readonly IStorageService _storageService;

    public CreatePostMediaUploadManager(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<IReadOnlyCollection<MediaUpload>> UploadManyAsync(
        Guid userId,
        ICollection<CreateMediaPost> files,
        CancellationToken cancellationToken)
    {
        var uploadTasks = files.Select(file => UploadMediaAsync(userId, file, cancellationToken));

        var uploadTasksResult = await Task.WhenAll(uploadTasks);
        if (uploadTasksResult.Any(result => result is false))
        {
            await RollbackAsync(userId);
        }

        return _uploadedMedias;
    }

    private async Task<bool> UploadMediaAsync(
        Guid userId,
        CreateMediaPost file,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = file.Media.OpenReadStream();

            var uploadedMedia = await _storageService.UploadMediaAsync(userId, stream, cancellationToken);
            if (uploadedMedia is null)
            {
                return false;
            }

            uploadedMedia.Description = file.Description;
            _uploadedMedias.Add(uploadedMedia);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task RollbackAsync(Guid userId)
    {
        var deleteMediaTasks = _uploadedMedias
            .Select(media => _storageService.DeleteMediaAsync(userId, media));

        await Task.WhenAll(deleteMediaTasks);

        _uploadedMedias.Clear();
    }
}

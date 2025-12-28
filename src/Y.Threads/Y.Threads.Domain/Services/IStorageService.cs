using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Domain.Services;
public interface IStorageService
{
    Task<MediaUpload?> UploadMediaAsync(Guid userId, Stream stream, CancellationToken cancellationToken = default);
    Task DeleteMediaAsync(Guid userId, MediaUpload media);
}

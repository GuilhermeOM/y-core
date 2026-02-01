using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Domain.Services;
public interface IStorageService
{
    Task<MediaUpload?> UploadAsync(Guid userId, Stream stream, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, MediaUpload media);
}

using Y.Core.SharedKernel;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Domain.Services;
public interface IStorageService
{
    Task<Result<FileUploadResult>> UploadAsync(
        FileUpload fileUpload,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string filePath);
}

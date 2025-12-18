
using Y.Threads.Domain.Models;

namespace Y.Threads.Domain.Services;
public interface IStorageService
{
    Task<Media?> UploadMediaAsync(Guid userId, Stream stream);
}

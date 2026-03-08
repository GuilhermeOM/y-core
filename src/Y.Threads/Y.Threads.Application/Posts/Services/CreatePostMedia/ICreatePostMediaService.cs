using Y.Core.SharedKernel;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Application.Posts.Services.CreatePostMedia;

internal interface ICreatePostMediaService
{
    Task<Result<FileUploadResult[]>> UploadManyAsync(
        Guid userId,
        ICollection<CreateMediaPost> medias,
        CancellationToken cancellationToken = default);

    Task RollbackAsync(IReadOnlyCollection<FileUploadResult> medias);
}

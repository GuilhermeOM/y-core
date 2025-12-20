using System.Net;
using Y.Core.SharedKernel;

namespace Y.Threads.Domain.Errors;
public static class PostErrors
{
    public static Error PostNotFound => new(HttpStatusCode.NotFound, "POST_NOT_FOUND", "Post not found");
    public static Error PostCreationFailed => new(HttpStatusCode.InternalServerError, "POST_CREATION_FAILED", "Post creation failed");
    public static Error MediaUploadFailed => new(HttpStatusCode.InternalServerError, "MEDIA_UPLOAD_FAILED", "Media upload failed");
}

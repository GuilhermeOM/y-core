using System.Net;
using Y.Core.SharedKernel;

namespace Y.Threads.Domain.Errors;
public static class PostErrors
{
    public static Error PostNotFound => new(HttpStatusCode.NotFound, "POST_NOT_FOUND", "Post not found");
    public static Error PostCreationFailed => new(HttpStatusCode.InternalServerError, "POST_CREATION_FAILED", "Post creation failed");
}

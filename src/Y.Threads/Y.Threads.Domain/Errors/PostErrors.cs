using System.Net;
using Y.Core.SharedKernel;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Domain.Errors;
public static class PostErrors
{
    public static Error EmptyAuthor => new("EMPTY_AUTHOR", "Author cannot be empty");
    public static Error EmptyPost => new("EMPTY_POST", "Post cannot be empty");
    public static Error UnsupportedMediaType => new("UNSUPPORTED_MEDIA_TYPE", "The media type is not supported");
    public static Error MaxMediaExceeded => new("MAX_MEDIA_EXCEEDED", $"A post cannot have more than {Post.MaxAllowedMedias} medias");

    public static Error PostNotFound => new(HttpStatusCode.NotFound, "POST_NOT_FOUND", "Post not found");
    public static Error PostCreationFailed => new(HttpStatusCode.InternalServerError, "POST_CREATION_FAILED", "Post creation failed");
    public static Error MediaUploadFailed => new(HttpStatusCode.InternalServerError, "MEDIA_UPLOAD_FAILED", "Media upload failed");
}

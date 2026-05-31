using System.Net;
using Y.Core.SharedKernel;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Domain.Errors;
public static class PostErrors
{
    public static Error EmptyAuthor => new(HttpStatusCode.UnprocessableEntity, "EMPTY_AUTHOR", "Author cannot be empty");
    public static Error EmptyPost => new(HttpStatusCode.UnprocessableEntity, "EMPTY_POST", "Post cannot be empty");
    public static Error UnsupportedMediaType => new(HttpStatusCode.UnprocessableEntity, "UNSUPPORTED_MEDIA_TYPE", "The media type is not supported");
    public static Error MaxMediaExceeded => new(HttpStatusCode.UnprocessableEntity, "MAX_MEDIA_EXCEEDED", $"A post cannot have more than {Post.MaxAllowedMedias} medias");
    public static Error LikeUnpublishedPost => new(HttpStatusCode.UnprocessableEntity, "LIKE_UNPUBLISHED_POST", "Post can not be liked when status is not published");
    public static Error LikeUndefinedPost => new(HttpStatusCode.UnprocessableEntity, "LIKE_UNDEFINED_POST", "Can not like an undefined post");
    public static Error LikeWithUndefinedUser => new(HttpStatusCode.UnprocessableEntity, "LIKE_WITH_UNDEFINED_USER", "Can not like a post with undefined user");
    public static Error PostNotFound => new(HttpStatusCode.NotFound, "POST_NOT_FOUND", "Post not found");
    public static Error PostCreationFailed => new(HttpStatusCode.InternalServerError, "POST_CREATION_FAILED", "Post creation failed");
    public static Error MediaUploadFailed => new(HttpStatusCode.InternalServerError, "MEDIA_UPLOAD_FAILED", "Media upload failed");
    public static Error PostReplyCreationFailed => new(HttpStatusCode.InternalServerError, "POST_REPLY_CREATION_FAILED", "Post reply creation failed");
}

public static class PostExceptions
{
    public sealed class PostAlreadyLikedException(string message) : Exception(message);
}

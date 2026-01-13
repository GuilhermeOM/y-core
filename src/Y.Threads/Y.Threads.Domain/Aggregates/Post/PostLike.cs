using Y.Core.SharedKernel;
using Y.Threads.Domain.Errors;

namespace Y.Threads.Domain.Aggregates.Post;
public class PostLike : Entity
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }

    public PostLike(Guid postId, Guid userId)
    {
        PostId = postId;
        UserId = userId;
    }

    public static Result<PostLike> Create(Guid postId, Guid userId)
    {
        if (postId == Guid.Empty)
        {
            return Result.Failure<PostLike>(PostErrors.LikeUndefinedPost);
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<PostLike>(PostErrors.LikeWithUndefinedUser);
        }

        return Result.Success(new PostLike(postId, userId));
    }
}

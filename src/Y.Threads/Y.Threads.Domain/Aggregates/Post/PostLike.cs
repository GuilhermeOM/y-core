using Y.Core.SharedKernel;

namespace Y.Threads.Domain.Aggregates.Post;
public class PostLike : Entity
{
    public Guid PostId { get; private set; }
    public Guid UserId { get; private set; }

    public PostLike(Guid postId, Guid userId)
    {
        Id = Guid.NewGuid();
        PostId = postId;
        UserId = userId;
    }

    internal static Result<PostLike> Create(Guid postId, Guid userId)
    {
        if (postId == Guid.Empty)
        {
            return Result.Failure<PostLike>(new Error("", ""));
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<PostLike>(new Error("", ""));
        }

        return Result.Success(new PostLike(postId, userId));
    }
}

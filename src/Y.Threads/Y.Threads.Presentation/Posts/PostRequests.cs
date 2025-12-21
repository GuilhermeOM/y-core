using Y.Threads.Application.Posts.UseCases.CreatePost;

namespace Y.Threads.Presentation.Posts;
public static class PostRequests
{
    public sealed record CreatePostRequest(string Text, ICollection<CreateMediaPost> Medias, Guid? Parent = null);
}

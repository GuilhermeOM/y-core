using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Application.Posts.Commands.ReplyPost;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Presentation.Posts;
public static class PostRequests
{
    public sealed record CreatePostRequest(string Text, IReadOnlyCollection<CreateMediaPost>? Medias, Guid? Parent = null);
    public sealed record ReplyPostRequest(string Text, IReadOnlyCollection<CreateMediaPost>? Medias);
}

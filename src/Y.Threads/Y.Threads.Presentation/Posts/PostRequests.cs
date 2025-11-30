using Microsoft.AspNetCore.Http;

namespace Y.Threads.Presentation.Posts;
public static class PostRequests
{
    public sealed record CreatePostRequest(string Text, IFormFile[]? Medias, Guid? Parent);
}

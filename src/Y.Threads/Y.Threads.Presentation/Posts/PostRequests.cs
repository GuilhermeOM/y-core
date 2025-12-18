using Microsoft.AspNetCore.Http;

namespace Y.Threads.Presentation.Posts;
public static class PostRequests
{
    public sealed record CreatePostRequest(string Text, List<IFormFile> Medias, Guid? Parent = null);
}

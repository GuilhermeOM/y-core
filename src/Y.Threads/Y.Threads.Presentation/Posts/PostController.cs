using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Posts.Commands.CreatePost;
using Y.Threads.Application.Posts.Commands.LikePost;
using Y.Threads.Application.Posts.Queries.GetPostById;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Presentation.Posts;

[Route("api/post")]
public sealed class PostController : ApiController
{
    private readonly IQueryHandler<GetPostByIdQuery, Post> _getPostByIdQueryHandler;
    private readonly ICommandHandler<CreatePostCommand, Guid> _createPostCommandHandler;
    private readonly ICommandHandler<LikePostCommand> _likePostCommandHandler;

    public PostController(
        IQueryHandler<GetPostByIdQuery, Post> getPostByIdQueryHandler,
        ICommandHandler<CreatePostCommand, Guid> createPostCommandHandler,
        ICommandHandler<LikePostCommand> likePostCommandHandler)
    {
        _getPostByIdQueryHandler = getPostByIdQueryHandler;
        _createPostCommandHandler = createPostCommandHandler;
        _likePostCommandHandler = likePostCommandHandler;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _getPostByIdQueryHandler.HandleAsync(new GetPostByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> CreateAsync(
        [FromForm] PostRequests.CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreatePostCommand
        {
            Author = GetAuthorFromAuthorization(),
            Text = request.Text,
            Medias = request.Medias,
            Parent = request.Parent ?? Guid.Empty
        };

        var result = await _createPostCommandHandler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Created("GetByIdAsync", new { Id = result.Value })
            : HandleFailure(result);
    }

    [HttpPost("like/{postId:guid}")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> LikeAsync(
        [FromRoute] Guid postId,
        CancellationToken cancellationToken = default)
    {
        var command = new LikePostCommand
        {
            UserId = GetAuthorFromAuthorization().Id,
            PostId = postId,
        };

        var result = await _likePostCommandHandler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }
}

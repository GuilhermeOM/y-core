using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Y.Core.SharedKernel.Abstractions;
using Y.Threads.Application.Posts.UseCases.CreatePost;
using Y.Threads.Application.Posts.UseCases.GetPostById;
using Y.Threads.Domain.Entities;
using Y.Threads.Domain.Models;

namespace Y.Threads.Presentation.Posts;

[Route("api/post")]
public sealed class PostController : ApiController
{
    private readonly IUseCaseHandler<GetPostByIdUseCase, Post> _getPostByIdUseCaseHandler;
    private readonly IUseCaseHandler<CreatePostUseCase, Guid> _createPostUseCaseHandler;

    public PostController(
        IUseCaseHandler<GetPostByIdUseCase, Post> getPostByIdUseCaseHandler,
        IUseCaseHandler<CreatePostUseCase, Guid> createPostUseCaseHandler)
    {
        _getPostByIdUseCaseHandler = getPostByIdUseCaseHandler;
        _createPostUseCaseHandler = createPostUseCaseHandler;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute]Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _getPostByIdUseCaseHandler.HandleAsync(new GetPostByIdUseCase(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> CreateAsync(
        [FromForm] PostRequests.CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        var usecase = new CreatePostUseCase
        {
            Author = GetAuthorFromAuthorization(),
            Text = request.Text,
            Medias = request.Medias,
            Parent = request.Parent ?? Guid.Empty
        };

        var result = await _createPostUseCaseHandler.HandleAsync(usecase, cancellationToken);

        return result.IsSuccess
            ? Created(nameof(GetByIdAsync), new { Id = result.Value })
            : HandleFailure(result);
    }
}

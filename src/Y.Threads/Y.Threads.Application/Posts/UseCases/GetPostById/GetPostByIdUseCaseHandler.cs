using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Entities;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.UseCases.GetPostById;
internal sealed class GetPostByIdUseCaseHandler : IUseCaseHandler<GetPostByIdUseCase, Post>
{
    private readonly IPostRepository _postRepository;

    public GetPostByIdUseCaseHandler(IPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<Result<Post>> HandleAsync(GetPostByIdUseCase request, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(request.Id, cancellationToken);
        if (post is null)
        {
            return Result.Failure<Post>(PostErrors.PostNotFound);
        }

        return Result.Success(post);
    }
}

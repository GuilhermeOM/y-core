using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Aggregates.Post;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.Queries.GetPostById;
internal sealed class GetPostByIdQueryHandler : IQueryHandler<GetPostByIdQuery, Post>
{
    private readonly IPostRepository _postRepository;

    public GetPostByIdQueryHandler(IPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<Result<Post>> HandleAsync(GetPostByIdQuery request, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(request.Id, cancellationToken);
        if (post is null)
        {
            return Result.Failure<Post>(PostErrors.PostNotFound);
        }

        return Result.Success(post);
    }
}

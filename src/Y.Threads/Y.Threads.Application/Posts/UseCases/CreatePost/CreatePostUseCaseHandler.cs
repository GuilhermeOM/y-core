using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions;
using Y.Threads.Domain.DomainEvents;
using Y.Threads.Domain.Entities;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Repositories;

namespace Y.Threads.Application.Posts.UseCases.CreatePost;
internal sealed class CreatePostUseCaseHandler : IUseCaseHandler<CreatePostUseCase, Guid>
{
    private readonly IPostRepository _postRepository;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;

    public CreatePostUseCaseHandler(
        IPostRepository postRepository,
        IDomainEventsDispatcher domainEventsDispatcher)
    {
        _postRepository = postRepository;
        _domainEventsDispatcher = domainEventsDispatcher;
    }

    public async Task<Result<Guid>> HandleAsync(CreatePostUseCase request, CancellationToken cancellationToken = default)
    {
        var post = new Post
        {
            Author = request.Author,
            Text = request.Text,
            Medias = [],
            Parent = request.Parent
        };

        var id = await _postRepository.CreateAsync(post, cancellationToken);
        if (id == Guid.Empty)
        {
            return Result.Failure<Guid>(PostErrors.PostCreationFailed);
        }

        await _domainEventsDispatcher.DispatchAsync(
        [
            new CreateThreadDomainEvent(post.Id, GetThreadType(post.Parent))
        ], cancellationToken);

        return Result.Success(id);
    }

    private static ThreadType GetThreadType(Guid parentId) => parentId == Guid.Empty ? ThreadType.Post : ThreadType.Reply;
}

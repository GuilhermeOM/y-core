using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Application.Posts.Queries.GetPostById;
public sealed record GetPostByIdQuery(Guid Id) : IQuery<Post>;

using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Models;

namespace Y.Threads.Application.Threads.Queries.GetThreadById;

public sealed record GetThreadByIdQuery(Guid Id, int MaxDepth) : IQuery<GetThreadByIdQueryResponse[]>;

public sealed record GetThreadByIdQueryResponse(
    Guid Id,
    AuthorSnapshot Author,
    string Text,
    IReadOnlyCollection<MediaSnapshot> Medias,
    long Depth,
    long LikeAmount,
    long ReplyAmount,
    DateTime CreatedAt);

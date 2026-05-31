using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Models;

namespace Y.Threads.Application.Threads.Queries.GetThreadById;

public sealed record GetThreadByIdQuery(Guid Id, int MaxDepth) : IQuery<Models.Thread[]>;


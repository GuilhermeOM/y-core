using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Threads.Abstractions;
using Y.Threads.Domain.Errors;

namespace Y.Threads.Application.Threads.Queries.GetThreadById;

internal sealed class GetThreadByIdQueryHandler : IQueryHandler<GetThreadByIdQuery, GetThreadByIdQueryResponse[]>
{
    private readonly IThreadRepository _threadRepository;

    public GetThreadByIdQueryHandler(IThreadRepository threadRepository)
    {
        _threadRepository = threadRepository;
    }

    public async Task<Result<GetThreadByIdQueryResponse[]>> HandleAsync(GetThreadByIdQuery request, CancellationToken cancellationToken = default)
    {
        var thread = await _threadRepository.GetByIdAndMaxDepthAsync(request.Id, request.MaxDepth, cancellationToken);
        if (!thread.Any())
        {
            return Result.Failure<GetThreadByIdQueryResponse[]>(ThreadErrors.ThreadNotFound);
        }

        return Result.Success(thread.ToArray());
    }
}

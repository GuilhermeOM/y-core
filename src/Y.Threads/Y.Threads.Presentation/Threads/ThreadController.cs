using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Y.Contract.SharedKernel.Enums;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Application.Posts.Queries.GetPostById;
using Y.Threads.Application.Threads.Queries.GetThreadById;

namespace Y.Threads.Presentation.Threads;

[Route("api/thread")]
[Authorize(Roles = nameof(Role.User))]
public sealed class ThreadController : ApiController
{
    private readonly IQueryHandler<GetThreadByIdQuery, Application.Threads.Models.Thread[]> _getThreadByIdQueryHandler;

    public ThreadController(IQueryHandler<GetThreadByIdQuery, Application.Threads.Models.Thread[]> getThreadByIdQueryHandler)
    {
        _getThreadByIdQueryHandler = getThreadByIdQueryHandler;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid id,
        [FromQuery] int maxDepth = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _getThreadByIdQueryHandler.HandleAsync(new GetThreadByIdQuery(id, maxDepth), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}

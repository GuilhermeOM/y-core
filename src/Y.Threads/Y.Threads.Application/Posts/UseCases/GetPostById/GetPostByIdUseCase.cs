using Y.Core.SharedKernel.Abstractions.Messaging;
using Y.Threads.Domain.Entities;

namespace Y.Threads.Application.Posts.UseCases.GetPostById;
public sealed record GetPostByIdUseCase(Guid Id) : IUseCase<Post>;

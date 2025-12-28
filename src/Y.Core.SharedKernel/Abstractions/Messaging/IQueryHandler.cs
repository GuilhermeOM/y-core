namespace Y.Core.SharedKernel.Abstractions.Messaging;
public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery request, CancellationToken cancellationToken = default);
}

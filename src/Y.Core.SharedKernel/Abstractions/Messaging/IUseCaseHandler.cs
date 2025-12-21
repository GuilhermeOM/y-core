namespace Y.Core.SharedKernel.Abstractions.Messaging;
public interface IUseCaseHandler<TRequest> where TRequest : IUseCase
{
    Task<Result> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IUseCaseHandler<TRequest, TResponse>
    where TRequest : IUseCase<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

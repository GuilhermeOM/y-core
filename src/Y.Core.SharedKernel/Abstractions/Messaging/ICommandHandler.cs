namespace Y.Core.SharedKernel.Abstractions.Messaging;
public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}


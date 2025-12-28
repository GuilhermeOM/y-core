namespace Y.Core.SharedKernel.Abstractions.Messaging;

public interface IBaseCommand;

public interface ICommand : IBaseCommand;

public interface ICommand<TResponse> : IBaseCommand
{
}



using Y.Core.SharedKernel;
using Serilog.Context;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace Y.Threads.Application.Abstractions.Behaviors;
internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> _innerHandler;
        private readonly ILogger<CommandHandler<TCommand>> _logger;

        public CommandHandler(
            ICommandHandler<TCommand> innerHandler,
            ILogger<CommandHandler<TCommand>> logger)
        {
            _innerHandler = innerHandler;
            _logger = logger;
        }

        public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            var commandName = typeof(TCommand).Name;

            using var _ = LogContext.PushProperty("CommandName", commandName);

            _logger.LogInformation("Processing command {CommandName}", commandName);

            var result = await _innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Completed command {CommandName}", commandName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Completed command {CommandName} with error", commandName);
                }
            }
            return result;
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private readonly ICommandHandler<TCommand, TResponse> _innerHandler;
        private readonly ILogger<CommandHandler<TCommand, TResponse>> _logger;

        public CommandHandler(
            ICommandHandler<TCommand, TResponse> innerHandler,
            ILogger<CommandHandler<TCommand, TResponse>> logger)
        {
            _innerHandler = innerHandler;
            _logger = logger;
        }

        public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            var commandName = typeof(TCommand).Name;

            using var _ = LogContext.PushProperty("CommandName", commandName);

            _logger.LogInformation("Processing command {CommandName}", commandName);

            var result = await _innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Completed command {CommandName}", commandName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Completed command {CommandName} with error", commandName);
                }
            }
            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        private readonly IQueryHandler<TQuery, TResponse> _innerHandler;
        private readonly ILogger<QueryHandler<TQuery, TResponse>> _logger;

        public QueryHandler(
            IQueryHandler<TQuery, TResponse> innerHandler,
            ILogger<QueryHandler<TQuery, TResponse>> logger)
        {
            _innerHandler = innerHandler;
            _logger = logger;
        }

        public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken)
        {
            var queryName = typeof(TQuery).Name;

            using var _ = LogContext.PushProperty("QueryName", queryName);

            _logger.LogInformation("Processing query {QueryName}", queryName);

            var result = await _innerHandler.HandleAsync(query, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Completed query {QueryName}", queryName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Completed query {QueryName} with error", queryName);
                }
            }
            return result;
        }
    }

    internal sealed class DomainEventHandler<TDomainEvent> : IDomainEventHandler<TDomainEvent> where TDomainEvent : IDomainEvent
    {
        private readonly IDomainEventHandler<TDomainEvent> _innerHandler;
        private readonly ILogger<DomainEventHandler<TDomainEvent>> _logger;

        public DomainEventHandler(
            IDomainEventHandler<TDomainEvent> innerHandler,
            ILogger<DomainEventHandler<TDomainEvent>> logger)
        {
            _innerHandler = innerHandler;
            _logger = logger;
        }

        public async Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var domainEventName = typeof(TDomainEvent).Name;

            using var _ = LogContext.PushProperty("DomainEventName", domainEventName);

            try
            {
                _logger.LogInformation("Processing domain event {DomainEventName}", domainEventName);

                await _innerHandler.HandleAsync(domainEvent, cancellationToken);

                _logger.LogInformation("Completed domain event {DomainEventName}", domainEventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing domain event {DomainEventName}", domainEventName);
                throw;
            }
        }
    }
}

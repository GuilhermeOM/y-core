using Y.Core.SharedKernel;
using Serilog.Context;
using Y.Core.SharedKernel.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace Y.Threads.Application.Abstractions.Behaviors;
internal static class LoggingDecorator
{
    internal sealed class UseCaseHandler<TRequest> : IUseCaseHandler<TRequest>
        where TRequest : IUseCase
    {
        private readonly IUseCaseHandler<TRequest> _innerHandler;
        private readonly ILogger<UseCaseHandler<TRequest>> _logger;

        public UseCaseHandler(
            IUseCaseHandler<TRequest> innerHandler,
            ILogger<UseCaseHandler<TRequest>> logger)
        {
            _innerHandler = innerHandler;
            _logger = logger;
        }

        public async Task<Result> HandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            var useCaseName = typeof(TRequest).Name;

            using var _ = LogContext.PushProperty("UseCaseName", useCaseName);

            _logger.LogInformation("Processing use case {UseCaseName}", useCaseName);

            var result = await _innerHandler.HandleAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Completed use case {UseCaseName}", useCaseName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Completed use case {UseCaseName} with error", useCaseName);
                }
            }
            return result;
        }
    }

    internal sealed class UseCaseHandler<TRequest, TResponse> : IUseCaseHandler<TRequest, TResponse>
        where TRequest : IUseCase<TResponse>
    {
        private readonly IUseCaseHandler<TRequest, TResponse> _innerHandler;
        private readonly ILogger<UseCaseHandler<TRequest, TResponse>> _logger;

        public UseCaseHandler(
            IUseCaseHandler<TRequest, TResponse> innerHandler,
            ILogger<UseCaseHandler<TRequest, TResponse>> logger)
        {
            _innerHandler = innerHandler;
            _logger = logger;
        }

        public async Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            var useCaseName = typeof(TRequest).Name;

            using var _ = LogContext.PushProperty("UseCaseName", useCaseName);

            _logger.LogInformation("Processing use case {UseCaseName}", useCaseName);

            var result = await _innerHandler.HandleAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Completed use case {UseCaseName}", useCaseName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Completed use case {UseCaseName} with error", useCaseName);
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

using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Application.Abstractions.Behaviors;
internal static class ValidationDecorator
{
    internal sealed class CommandValidationHandler<TRequest> : ICommandHandler<TRequest>
    where TRequest : ICommand
    {
        private readonly ICommandHandler<TRequest> _innerHandler;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public CommandValidationHandler(
            ICommandHandler<TRequest> innerHandler,
            IEnumerable<IValidator<TRequest>> validators)
        {
            _innerHandler = innerHandler;
            _validators = validators;
        }

        public async Task<Result> HandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            var validationFailures = await ValidateAsync(request, _validators);

            if (validationFailures.Length == 0)
            {
                return await _innerHandler.HandleAsync(request, cancellationToken);
            }

            return Result.Failure(CreateValidationError(validationFailures));
        }
    }

    internal sealed class CommandValidationHandler<TRequest, TResponse> : ICommandHandler<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
    {
        private readonly ICommandHandler<TRequest, TResponse> _innerHandler;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public CommandValidationHandler(
            ICommandHandler<TRequest, TResponse> innerHandler,
            IEnumerable<IValidator<TRequest>> validators)
        {
            _innerHandler = innerHandler;
            _validators = validators;
        }

        public async Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            var validationFailures = await ValidateAsync(request, _validators);

            if (validationFailures.Length == 0)
            {
                return await _innerHandler.HandleAsync(request, cancellationToken);
            }

            return Result.Failure<TResponse>(CreateValidationError(validationFailures));
        }
    }

    internal sealed class QueryValidationHandler<TRequest, TResponse> : IQueryHandler<TRequest, TResponse>
        where TRequest : IQuery<TResponse>
    {
        private readonly IQueryHandler<TRequest, TResponse> _innerHandler;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public QueryValidationHandler(
            IQueryHandler<TRequest, TResponse> innerHandler,
            IEnumerable<IValidator<TRequest>> validators)
        {
            _innerHandler = innerHandler;
            _validators = validators;
        }

        public async Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            var validationFailures = await ValidateAsync(request, _validators);

            if (validationFailures.Length == 0)
            {
                return await _innerHandler.HandleAsync(request, cancellationToken);
            }

            return Result.Failure<TResponse>(CreateValidationError(validationFailures));
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<TRequest>(TRequest request, IEnumerable<IValidator<TRequest>> validators)
    {
        if (!validators.Any())
        {
            return [];
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, CancellationToken.None)));

        var validationFailures = validationResults
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .ToArray();

        return validationFailures;
    }

    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures)
    {
        var errors = validationFailures
            .Select(failure => new Error(HttpStatusCode.BadRequest, failure.ErrorCode, failure.ErrorMessage))
            .ToArray();

        return new(errors);
    }
}

using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Abstractions.Messaging;

namespace Y.Threads.Application.Abstractions.Behaviors;
internal static class ValidationDecorator
{
    internal sealed class ValidationHandler<TRequest> : IUseCaseHandler<TRequest>
    where TRequest : IUseCase
    {
        private readonly IUseCaseHandler<TRequest> _innerHandler;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationHandler(
            IUseCaseHandler<TRequest> innerHandler,
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

    internal sealed class ValidationHandler<TRequest, TResponse> : IUseCaseHandler<TRequest, TResponse>
        where TRequest : IUseCase<TResponse>
    {
        private readonly IUseCaseHandler<TRequest, TResponse> _innerHandler;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationHandler(
            IUseCaseHandler<TRequest, TResponse> innerHandler,
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

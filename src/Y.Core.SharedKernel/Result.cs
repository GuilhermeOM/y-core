namespace Y.Core.SharedKernel;
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public Error[] Errors { get; } = [];

    protected Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
        Errors = [error];
    }

    protected Result(Error[] errors) : this(false, errors.FirstOrDefault() ?? Error.None)
    {
        Errors = errors;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(ValidationError validationError) => new(validationError.Errors);
    public static Result<TValue> Success<TValue>(TValue value) => new(true, Error.None, value);
    public static Result<TValue> Failure<TValue>(Error error) => new(false, error, default);
    public static Result<TValue> Failure<TValue>(ValidationError validationError) => new(validationError.Errors);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    protected internal Result(bool isSuccess, Error error, TValue? value) : base(isSuccess, error)
    {
        _value = value;
    }

    protected internal Result(Error[] errors) : base(errors)
    {
    }
}

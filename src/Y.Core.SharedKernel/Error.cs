using System.Net;

namespace Y.Core.SharedKernel;
public sealed record Error
{
    private readonly HttpStatusCode _httpStatusCode = HttpStatusCode.InternalServerError;

    public string Code { get; }
    public string Description { get; }

    public static Error None => new(string.Empty, string.Empty);

    public Error(string code, string description = "")
    {
        Code = code;
        Description = description;
    }

    public Error(HttpStatusCode statusCode, string code, string description = "") : this(code, description)
    {
        if (statusCode < HttpStatusCode.BadRequest)
        {
            throw new ArgumentException("Status code must be greater or equal to 400", nameof(statusCode));
        }

        _httpStatusCode = statusCode;
    }

    public HttpStatusCode GetStatusCode() => _httpStatusCode;
}

public class ValidationError
{
    public Error[] Errors { get; } = [];

    public ValidationError(Error[] errors)
    {
        if (errors == null || errors.Length == 0)
        {
            throw new ArgumentException("Errors cannot be null or empty", nameof(errors));
        }

        Errors = errors;
    }
}

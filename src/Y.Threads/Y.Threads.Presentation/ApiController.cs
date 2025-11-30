using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Y.Core.SharedKernel;
using Y.Threads.Domain.Entities;

namespace Y.Threads.Presentation;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult HandleFailure(Result result)
    {
        try
        {
            return result.Error.GetStatusCode() switch
            {
                HttpStatusCode.OK => throw new InvalidOperationException("200 is not a valid error"),
                HttpStatusCode.Unauthorized => Unauthorized(),
                HttpStatusCode.BadRequest => BadRequest(new ErrorDetailsResponse(nameof(StatusCodes.Status400BadRequest), StatusCodes.Status400BadRequest, result.Errors)),
                HttpStatusCode.NotFound => NotFound(new ErrorDetailsResponse(nameof(StatusCodes.Status404NotFound), StatusCodes.Status404NotFound, result.Errors)),
                HttpStatusCode.Conflict => Conflict(new ErrorDetailsResponse(nameof(StatusCodes.Status409Conflict), StatusCodes.Status409Conflict, result.Errors)),
                _ => CreateInternalServerError(result.Errors)
            };
        }
        catch (Exception)
        {
            var error = new Error(HttpStatusCode.InternalServerError, "An internal error occurred");
            return CreateInternalServerError([error]);
        }
    }

    private ObjectResult CreateInternalServerError(Error[] errors) => StatusCode(
        StatusCodes.Status500InternalServerError,
        new ErrorDetailsResponse(nameof(StatusCodes.Status500InternalServerError), StatusCodes.Status500InternalServerError, errors));

    protected Author GetAuthorFromAuthorization() => new()
    {
        Id = Guid.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString()),
        Name = HttpContext.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? string.Empty,
        Email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
        Birthdate = DateOnly.Parse(HttpContext.User.FindFirst(ClaimTypes.DateOfBirth)?.Value ?? DateOnly.MinValue.ToString())
    };
}

internal sealed record ErrorDetailsResponse(string Title, int Status, Error[] Errors);

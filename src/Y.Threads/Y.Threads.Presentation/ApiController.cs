using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Models;

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
                HttpStatusCode.BadRequest => StatusCode(HttpStatusCode.BadRequest, nameof(StatusCodes.Status400BadRequest), result.Errors),
                HttpStatusCode.NotFound => StatusCode(HttpStatusCode.NotFound, nameof(StatusCodes.Status404NotFound), result.Errors),
                HttpStatusCode.Conflict => StatusCode(HttpStatusCode.Conflict, nameof(StatusCodes.Status409Conflict), result.Errors),
                _ => StatusCode(HttpStatusCode.InternalServerError, nameof(StatusCodes.Status500InternalServerError), result.Errors)
            };
        }
        catch (Exception)
        {
            var error = new Error(HttpStatusCode.InternalServerError, nameof(StatusCodes.Status500InternalServerError), "An internal error occurred");
            return StatusCode(error.GetStatusCode(), error.Code, [error]);
        }
    }

    private ObjectResult StatusCode(HttpStatusCode statusCode, string title, Error[] errors)
    {
        var intStatusCode = (int)statusCode;
        return StatusCode(intStatusCode, new ErrorDetailsResponse(title, statusCode, errors));
    }

    protected Author GetAuthorFromAuthorization()
    {
        var user = HttpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString();

        return new()
        {
            Id = Guid.Parse(userId),
            Name = user.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? string.Empty,
            Email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            Birthdate = DateOnly.Parse(user.FindFirst(ClaimTypes.DateOfBirth)?.Value ?? DateOnly.MinValue.ToString()),
            AvatarUrl = user.FindFirst("avatarUrl")?.Value ?? string.Empty
        };
    }
}

internal sealed record ErrorDetailsResponse(string Title, HttpStatusCode Status, Error[] Errors);

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Verifier.Exceptions;

namespace Dialysis.IdentityAdmission;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        httpContext.Response.ContentType = "application/json";

        var body = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Validation failed",
            status = 400,
            errors = validationException.Errors.Select(e => new { property = e.PropertyName, message = e.ErrorMessage }).ToArray()
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions), cancellationToken);
        return true;
    }
}

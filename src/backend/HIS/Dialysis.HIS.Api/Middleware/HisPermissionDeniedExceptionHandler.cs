using System.Net;
using System.Text.Json;
using Dialysis.HIS.Security;
using Microsoft.AspNetCore.Diagnostics;

namespace Dialysis.HIS.Api.Middleware;

internal sealed class HisPermissionDeniedExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not HisPermissionDeniedException ex)
            return false;

        httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        var body = JsonSerializer.Serialize(
            new
            {
                title = "Forbidden",
                status = 403,
                detail = ex.Message,
                permission = ex.Permission,
            },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await httpContext.Response.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        return true;
    }
}

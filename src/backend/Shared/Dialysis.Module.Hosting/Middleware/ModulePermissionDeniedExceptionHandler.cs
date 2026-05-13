using System.Net;
using System.Text.Json;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Dialysis.Module.Hosting.Middleware;

internal sealed class ModulePermissionDeniedExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ModulePermissionDeniedException ex)
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
            JsonOptions);
        await httpContext.Response.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        return true;
    }
}

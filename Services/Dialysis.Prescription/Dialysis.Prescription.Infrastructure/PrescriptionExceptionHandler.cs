using BuildingBlocks.ExceptionHandling;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Dialysis.Prescription.Application.Exceptions;

namespace Dialysis.Prescription.Infrastructure;

/// <summary>
/// Handles Prescription-specific exceptions (RspK22ValidationException, PrescriptionConflictException).
/// Returns RFC 7807 Problem Details. Unhandled exceptions fall through to CentralExceptionHandler.
/// </summary>
public sealed class PrescriptionExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is RspK22ValidationException rspEx)
        {
            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "RSP^K22 Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = rspEx.Message,
                Instance = httpContext.Request.Path,
                Extensions = { ["errorCode"] = rspEx.ErrorCode },
            };
            await WriteProblemDetailsAsync(httpContext, problem, StatusCodes.Status400BadRequest, cancellationToken);
            return true;
        }

        if (exception is PrescriptionConflictException conflictEx)
        {
            var extensions = new Dictionary<string, object?> { ["orderId"] = conflictEx.OrderId };
            if (!string.IsNullOrEmpty(conflictEx.CallbackPhone))
                extensions["callbackPhone"] = conflictEx.CallbackPhone;
            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Prescription Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = conflictEx.Message,
                Instance = httpContext.Request.Path,
                Extensions = extensions,
            };
            await WriteProblemDetailsAsync(httpContext, problem, StatusCodes.Status409Conflict, cancellationToken);
            return true;
        }

        return false;
    }

    private static async Task WriteProblemDetailsAsync(HttpContext httpContext, ProblemDetails problem, int statusCode, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(ProblemDetailsFactory.ToJson(problem), cancellationToken);
    }
}

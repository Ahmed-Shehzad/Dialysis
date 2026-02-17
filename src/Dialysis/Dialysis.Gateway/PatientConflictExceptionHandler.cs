using Dialysis.SharedKernel.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway;

public sealed class PatientConflictExceptionHandler : IExceptionHandler
{
    private readonly ILogger<PatientConflictExceptionHandler> _logger;

    public PatientConflictExceptionHandler(ILogger<PatientConflictExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not PatientAlreadyExistsException conflict)
            return false;

        _logger.LogWarning("Patient conflict: {Message}", conflict.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = conflict.Message
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

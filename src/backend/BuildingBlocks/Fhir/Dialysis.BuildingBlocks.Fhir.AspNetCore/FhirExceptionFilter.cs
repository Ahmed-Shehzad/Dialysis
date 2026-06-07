using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

/// <summary>
/// Translates uncaught exceptions thrown inside FHIR endpoint handlers into
/// <c>OperationOutcome</c> responses with appropriate HTTP status codes.
/// </summary>
public sealed class FhirExceptionFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (statusCode, outcome) = context.Exception switch
        {
            KeyNotFoundException knfe => (
                StatusCodes.Status404NotFound,
                OperationOutcomeFactory.NotFound("Resource", knfe.Message)),

            UnauthorizedAccessException uae => (
                StatusCodes.Status403Forbidden,
                OperationOutcomeFactory.Forbidden(uae.Message)),

            ArgumentException ae => (
                StatusCodes.Status400BadRequest,
                OperationOutcomeFactory.BadRequest([new FhirError("invalid", ae.Message)])),

            _ => (
                StatusCodes.Status500InternalServerError,
                OperationOutcomeFactory.FromException(context.Exception)),
        };

        context.ExceptionHandled = true;
        context.Result = new FhirContentResult(outcome, statusCode);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}

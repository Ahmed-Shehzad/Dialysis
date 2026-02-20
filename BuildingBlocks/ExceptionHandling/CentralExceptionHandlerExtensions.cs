using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

using Verifier.Exceptions;

namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Central exception handling for APIs. Handles ValidationException, ArgumentException.
/// </summary>
public static class CentralExceptionHandlerExtensions
{
    /// <summary>
    /// Uses a central exception handler that returns 400 for ValidationException and ArgumentException,
    /// 500 for unhandled exceptions.
    /// </summary>
    public static IApplicationBuilder UseCentralExceptionHandler(this IApplicationBuilder app)
    {
        _ = app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is ValidationException validationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    var errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                    await context.Response.WriteAsJsonAsync(new { errors });
                    return;
                }
                if (exception is ArgumentException argEx)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { message = argEx.Message });
                    return;
                }
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            });
        });
        return app;
    }
}

using System.Diagnostics;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Verifier.Exceptions;

namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Builds RFC 7807 Problem Details from exceptions and HTTP context.
/// </summary>
public static class ProblemDetailsFactory
{
    private const string StackTraceKey = "stackTrace";
    private const string ErrorsKey = "errors";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Creates a Problem Details response for the given exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="includeStackTrace">Whether to include the exception stack trace in the response.</param>
    /// <returns>A ProblemDetails instance and the HTTP status code.</returns>
    public static (ProblemDetails Problem, int StatusCode) Create(Exception exception, HttpContext httpContext, bool includeStackTrace)
    {
        return exception switch
        {
            ValidationException validationException => CreateValidationProblem(validationException, httpContext, includeStackTrace),
            ArgumentException argEx => CreateArgumentProblem(argEx, httpContext, includeStackTrace),
            KeyNotFoundException => CreateNotFoundProblem(exception, httpContext, includeStackTrace),
            _ => CreateInternalServerProblem(exception, httpContext, includeStackTrace),
        };
    }

    private static (ProblemDetails Problem, int StatusCode) CreateValidationProblem(
        ValidationException validationException,
        HttpContext httpContext,
        bool includeStackTrace)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = validationException.Message,
            Instance = httpContext.Request.Path,
            Extensions = { [ErrorsKey] = validationException.Errors.Select(e => new { e.PropertyName, ErrorMessage = e.ErrorMessage }).ToArray() },
        };
        AddTraceId(httpContext, problem);
        if (includeStackTrace)
            problem.Extensions[StackTraceKey] = validationException.StackTrace;
        return (problem, StatusCodes.Status400BadRequest);
    }

    private static (ProblemDetails Problem, int StatusCode) CreateArgumentProblem(
        ArgumentException argEx,
        HttpContext httpContext,
        bool includeStackTrace)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = argEx.Message,
            Instance = httpContext.Request.Path,
        };
        AddTraceId(httpContext, problem);
        if (includeStackTrace)
            problem.Extensions[StackTraceKey] = argEx.StackTrace;
        return (problem, StatusCodes.Status400BadRequest);
    }

    private static (ProblemDetails Problem, int StatusCode) CreateNotFoundProblem(
        Exception exception,
        HttpContext httpContext,
        bool includeStackTrace)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };
        AddTraceId(httpContext, problem);
        if (includeStackTrace)
            problem.Extensions[StackTraceKey] = exception.StackTrace;
        return (problem, StatusCodes.Status404NotFound);
    }

    private static (ProblemDetails Problem, int StatusCode) CreateInternalServerProblem(
        Exception exception,
        HttpContext httpContext,
        bool includeStackTrace)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occurred processing your request.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };
        AddTraceId(httpContext, problem);
        if (includeStackTrace)
            problem.Extensions[StackTraceKey] = exception.ToString();
        return (problem, StatusCodes.Status500InternalServerError);
    }

    private static void AddTraceId(HttpContext httpContext, ProblemDetails problem)
    {
        string? traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
            problem.Extensions["traceId"] = traceId;
    }

    /// <summary>
    /// Serializes ProblemDetails to JSON for the response body.
    /// </summary>
    public static string ToJson(ProblemDetails problem) =>
        JsonSerializer.Serialize(problem, JsonOptions);
}

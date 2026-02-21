using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Verifier.Exceptions;

namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Central exception handler for APIs. Returns RFC 7807 Problem Details.
/// Includes stack trace only in Development; in Production, emails full report to dev inbox.
/// </summary>
public sealed class CentralExceptionHandler : IExceptionHandler
{
    private static readonly string[] SensitiveHeaders = ["Authorization", "Cookie", "X-Api-Key", "Api-Key"];

    private readonly IWebHostEnvironment _env;
    private readonly IExceptionReportEmailSender _emailSender;

    public CentralExceptionHandler(IWebHostEnvironment env, IExceptionReportEmailSender emailSender)
    {
        _env = env;
        _emailSender = emailSender;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        bool includeStackTraceInResponse = string.Equals(_env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);
        (Microsoft.AspNetCore.Mvc.ProblemDetails problem, int statusCode) = ProblemDetailsFactory.Create(
            exception, httpContext, includeStackTraceInResponse);

        string responseBody = ProblemDetailsFactory.ToJson(problem);
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(responseBody, cancellationToken);

        if (!includeStackTraceInResponse)
        {
            (Microsoft.AspNetCore.Mvc.ProblemDetails fullProblem, _) = ProblemDetailsFactory.Create(
                exception, httpContext, includeStackTrace: true);
            string fullProblemJson = ProblemDetailsFactory.ToJson(fullProblem);
            ExceptionReport report = BuildReport(exception, httpContext, statusCode, fullProblemJson);
            _ = Task.Run(() => _emailSender.SendAsync(report, CancellationToken.None), CancellationToken.None);
        }

        return true;
    }

    private static ExceptionReport BuildReport(Exception exception, HttpContext httpContext, int statusCode, string fullProblemDetailsJson)
    {
        var requestSnapshot = new RequestSnapshot
        {
            Method = httpContext.Request.Method,
            Path = httpContext.Request.Path,
            QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null,
            Headers = GetSanitizedHeaders(httpContext.Request.Headers),
        };

        var responseSnapshot = new ResponseSnapshot
        {
            StatusCode = statusCode,
            Body = fullProblemDetailsJson,
        };

        ExceptionSnapshot exceptionSnapshot = ExceptionSnapshotBuilder.Build(exception);

        return new ExceptionReport
        {
            OccurredAt = DateTimeOffset.UtcNow,
            Environment = (httpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment)?.EnvironmentName ?? "Unknown",
            Request = requestSnapshot,
            Response = responseSnapshot,
            Exception = exceptionSnapshot,
        };
    }

    private static IReadOnlyDictionary<string, string> GetSanitizedHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in headers)
        {
            string key = header.Key;
            if (SensitiveHeaders.Contains(key, StringComparer.OrdinalIgnoreCase))
                result[key] = "[REDACTED]";
            else
                result[key] = header.Value.ToString();
        }
        return result;
    }
}

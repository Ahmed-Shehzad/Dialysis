using System.Text.Json.Serialization;

namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Full error report for email delivery in non-Development environments.
/// Includes request, response (Problem Details with stack trace), and exception details.
/// </summary>
public sealed record ExceptionReport
{
    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("environment")]
    public string Environment { get; init; } = string.Empty;

    [JsonPropertyName("request")]
    public RequestSnapshot Request { get; init; } = null!;

    [JsonPropertyName("response")]
    public ResponseSnapshot Response { get; init; } = null!;

    [JsonPropertyName("exception")]
    public ExceptionSnapshot Exception { get; init; } = null!;
}

/// <summary>
/// Sanitized snapshot of the HTTP request (sensitive headers redacted).
/// </summary>
public sealed record RequestSnapshot
{
    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("queryString")]
    public string? QueryString { get; init; }

    [JsonPropertyName("headers")]
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Snapshot of the HTTP response (Problem Details body).
/// </summary>
public sealed record ResponseSnapshot
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;
}

/// <summary>
/// Complete snapshot of the exception (type, message, stack trace, inner exceptions, data).
/// </summary>
public sealed record ExceptionSnapshot
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("helpLink")]
    public string? HelpLink { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>();

    [JsonPropertyName("innerException")]
    public ExceptionSnapshot? InnerException { get; init; }

    [JsonPropertyName("toString")]
    public string ToStringOutput { get; init; } = string.Empty;
}

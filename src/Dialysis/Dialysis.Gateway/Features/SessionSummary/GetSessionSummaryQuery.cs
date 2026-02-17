using global::Hl7.Fhir.Model;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.SessionSummary;

/// <summary>
/// Builds a FHIR session summary bundle for a completed session. Optionally saves to file.
/// </summary>
public sealed record GetSessionSummaryQuery(
    string SessionId,
    string BaseUrl,
    string? SaveToFilePath = null
) : IQuery<GetSessionSummaryResult>;

public sealed record GetSessionSummaryResult(Bundle? Bundle, string? Json, string? Error);

using System.Text.Json.Serialization;
using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for Alerting service.</summary>
public interface IAlertingApi
{
    [Get("/api/v1/alerts")]
    Task<IReadOnlyList<AlertDto>> GetAlertsAsync(CancellationToken cancellationToken = default);
}

/// <summary>DTO for alert list item from Alerting API.</summary>
public sealed class AlertDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("patientId")] public string PatientId { get; init; } = "";
    [JsonPropertyName("encounterId")] public string EncounterId { get; init; } = "";
    [JsonPropertyName("code")] public string Code { get; init; } = "";
    [JsonPropertyName("severity")] public string Severity { get; init; } = "";
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("raisedAt")] public DateTimeOffset RaisedAt { get; init; }
    [JsonPropertyName("acknowledgedAt")] public DateTimeOffset? AcknowledgedAt { get; init; }
}

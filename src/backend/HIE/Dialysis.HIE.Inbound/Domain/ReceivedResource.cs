namespace Dialysis.HIE.Inbound.Domain;

/// <summary>
/// One FHIR resource accepted from a partner. Idempotent on <see cref="PartnerId"/> + <see cref="ResourceType"/> + <see cref="LogicalId"/>.
/// </summary>
public sealed class ReceivedResource
{
    public Guid Id { get; private set; }
    public string PartnerId { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string LogicalId { get; private set; } = string.Empty;
    public string FhirJson { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public string? ValidationOutcome { get; private set; }

    private ReceivedResource() { }

    public ReceivedResource(string partnerId, string resourceType, string logicalId, string fhirJson, DateTime receivedAtUtc, string? validationOutcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fhirJson);
        Id = Guid.NewGuid();
        PartnerId = partnerId;
        ResourceType = resourceType;
        LogicalId = logicalId;
        FhirJson = fhirJson;
        ReceivedAtUtc = receivedAtUtc;
        ValidationOutcome = validationOutcome;
    }
}

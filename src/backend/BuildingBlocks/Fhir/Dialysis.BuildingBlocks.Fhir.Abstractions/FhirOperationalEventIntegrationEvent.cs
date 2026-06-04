namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirOperationalEventIntegrationEvent
{
    public FhirOperationalEventIntegrationEvent(string Operation,
        string ResourceType,
        string? PatientReference,
        FhirIssueSeverity Severity,
        string? Code,
        string? Message,
        string CorrelationId,
        DateTimeOffset OccurredAt,
        int SchemaVersion = 1)
    {
        this.Operation = Operation;
        this.ResourceType = ResourceType;
        this.PatientReference = PatientReference;
        this.Severity = Severity;
        this.Code = Code;
        this.Message = Message;
        this.CorrelationId = CorrelationId;
        this.OccurredAt = OccurredAt;
        this.SchemaVersion = SchemaVersion;
    }
    public string Operation { get; init; }
    public string ResourceType { get; init; }
    public string? PatientReference { get; init; }
    public FhirIssueSeverity Severity { get; init; }
    public string? Code { get; init; }
    public string? Message { get; init; }
    public string CorrelationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public int SchemaVersion { get; init; }
    public void Deconstruct(out string Operation, out string ResourceType, out string? PatientReference, out FhirIssueSeverity Severity, out string? Code, out string? Message, out string CorrelationId, out DateTimeOffset OccurredAt, out int SchemaVersion)
    {
        Operation = this.Operation;
        ResourceType = this.ResourceType;
        PatientReference = this.PatientReference;
        Severity = this.Severity;
        Code = this.Code;
        Message = this.Message;
        CorrelationId = this.CorrelationId;
        OccurredAt = this.OccurredAt;
        SchemaVersion = this.SchemaVersion;
    }
}

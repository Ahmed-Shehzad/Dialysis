namespace Dialysis.HisIntegration.Features.AdtSync;

public sealed record AdtParsedData
{
    public required string MessageType { get; init; }
    public string? Mrn { get; init; }
    public string? FamilyName { get; init; }
    public string? GivenName { get; init; }
    public string? BirthDate { get; init; }
    public string? Gender { get; init; }
    public string? AdmitDateTime { get; init; }
    public string? DischargeDateTime { get; init; }
    public string? EncounterId { get; init; }
    public string? Ward { get; init; }
    public string? AttendingPhysician { get; init; }
}

namespace Dialysis.HisIntegration.Features.AdtSync;

public interface IFhirAdtWriter
{
    Task<(string? PatientId, string? EncounterId)> WriteAdtAsync(AdtParsedData data, CancellationToken cancellationToken = default);
}

namespace Dialysis.Treatment.Application.Abstractions;

/// <summary>
/// Builds HL7 ACK^R01 acknowledgment messages for PCD-01 (ORU^R01) observations.
/// </summary>
public interface IAckR01Builder
{
    string BuildAccept(string messageControlId);

    string BuildError(string messageControlId, string errorText);
}

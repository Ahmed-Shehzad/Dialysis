namespace Dialysis.Alarm.Application.Abstractions;

/// <summary>
/// Builds HL7 ORA^R41 acknowledgment messages for PCD-04 (ORU^R40) alarm reports.
/// </summary>
public interface IOraR41Builder
{
    string BuildAccept(string messageControlId);

    string BuildError(string messageControlId, string errorText);
}

using Dialysis.BuildingBlocks.Fhir.DeIdentification;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Maps the export job's de-identification request (the <c>_deIdentify</c> parameter, persisted on
/// <see cref="ExportJob.DeIdentificationProfile"/>) to a <see cref="DeIdentificationProfile"/>.
/// Fail-closed: an unrecognised non-empty value throws so the runner fails the job rather than
/// streaming identified PHI under a profile name it doesn't understand.
/// </summary>
public static class ExportDeIdentification
{
    /// <summary>
    /// Resolves the requested profile. Returns <see langword="null"/> when no de-identification was
    /// requested; throws <see cref="FormatException"/> for an unrecognised profile name (fail-closed).
    /// </summary>
    public static DeIdentificationProfile? ResolveProfile(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return null;
        }

        return requested.Trim().ToLowerInvariant() switch
        {
            "safeharbor" or "safe-harbor" or "true" or "1" or "yes" => DeIdentificationProfile.SafeHarbor,
            "limiteddataset" or "limited-data-set" or "lds" => DeIdentificationProfile.LimitedDataSet,
            "custom" => DeIdentificationProfile.Custom,
            _ => throw new FormatException(
                $"Unrecognised de-identification profile '{requested}'. Expected SafeHarbor, LimitedDataSet, or Custom."),
        };
    }
}

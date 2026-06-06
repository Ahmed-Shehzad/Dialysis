using Dialysis.HIE.Inbound.Mpi;

namespace Dialysis.HIE.Query;

/// <summary>One candidate the partner returned for a patient-discovery query.</summary>
public sealed record DiscoveredPatient(string PartnerPatientId, string? DisplayName, decimal? Score);

/// <summary>
/// Cross-community patient discovery (XCPD-style): resolves the partner-side patient id(s) for a
/// patient described by demographics, by querying the partner's <c>Patient</c> search. The resolved
/// id is what a subsequent <see cref="IPartnerFhirQuery"/> pull needs as its query target.
/// </summary>
public interface IPartnerPatientDiscovery
{
    Task<IReadOnlyList<DiscoveredPatient>> DiscoverAsync(
        Guid partnerId, PatientMatchCriteria criteria, string subject, string purposeOfUse, CancellationToken cancellationToken = default);
}

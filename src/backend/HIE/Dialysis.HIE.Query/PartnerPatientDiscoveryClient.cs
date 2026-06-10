using Dialysis.HIE.Inbound.Mpi;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Query;

/// <summary>
/// Resolves a patient at a partner by running a demographic <c>Patient</c> search over the outbound
/// FHIR query client (so it inherits the partner resolution + purpose-scoped IAS JWT auth). Projects
/// the returned <see cref="Patient"/> resources to their logical ids + a display name.
/// </summary>
public sealed class PartnerPatientDiscoveryClient : IPartnerPatientDiscovery
{
    private readonly IPartnerFhirQuery _query;
    public PartnerPatientDiscoveryClient(IPartnerFhirQuery query) => _query = query;

    public async Task<IReadOnlyList<DiscoveredPatient>> DiscoverAsync(
        Guid partnerId, PatientMatchCriteria criteria, string subject, string purposeOfUse, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var query = BuildSearch(criteria);

        var resources = await _query.QueryAsync(partnerId, query, subject, purposeOfUse, cancellationToken).ConfigureAwait(false);
        return resources
            .OfType<Patient>()
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .Select(p => new DiscoveredPatient(p.Id!, DisplayName(p), Score: null))
            .ToList();
    }

    private static string BuildSearch(PatientMatchCriteria criteria)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(criteria.Mrn))
            parameters.Add($"identifier={Uri.EscapeDataString(criteria.Mrn)}");
        if (!string.IsNullOrWhiteSpace(criteria.FamilyName))
            parameters.Add($"family={Uri.EscapeDataString(criteria.FamilyName)}");
        if (!string.IsNullOrWhiteSpace(criteria.GivenName))
            parameters.Add($"given={Uri.EscapeDataString(criteria.GivenName)}");
        if (criteria.DateOfBirth is { } dob)
            parameters.Add($"birthdate={dob:yyyy-MM-dd}");
        // A demographics-free discovery would return the partner's whole census — refuse it.
        if (parameters.Count == 0)
            throw new InvalidOperationException("Patient discovery requires at least one demographic criterion.");
        return "Patient?" + string.Join('&', parameters);
    }

    private static string? DisplayName(Patient patient)
    {
        var name = patient.Name.FirstOrDefault();
        if (name is null)
            return null;
        var given = string.Join(' ', name.Given);
        return string.Join(", ", new[] { name.Family, given }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
    }
}

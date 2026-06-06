using Dialysis.EHR.Registration.Domain;

namespace Dialysis.EHR.Registration.Ports;

/// <summary>Persistence for the per-patient <see cref="CareTeam"/> roster (members included).</summary>
public interface ICareTeamRepository
{
    /// <summary>Returns the patient's care team (members included), or null if none exists yet.</summary>
    Task<CareTeam?> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    void Add(CareTeam careTeam);
}

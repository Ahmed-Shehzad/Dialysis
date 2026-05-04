using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.Persistence.PatientAccess;

/// <summary>Always-allow stub for local tests. The host registers <c>RuleBasedPatientConsentGate</c> + EF read model by default.</summary>
public sealed class PermissivePatientConsentGate : IPatientConsentGate
{
    public Task EnsureCanViewSummaryAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

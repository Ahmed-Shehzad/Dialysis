namespace Dialysis.HIS.Contracts.PatientLifecycle;

/// <summary>
/// Allows other bounded contexts to react after a patient is persisted (portal consent defaults, search index hooks, etc.).
/// </summary>
public interface IPatientRegisteredLifecycleHook
{
    Task AfterPatientRegisteredAsync(Guid patientId, CancellationToken cancellationToken = default);
}

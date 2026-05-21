using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>
/// Cross-module policy: "When a patient is checked in at the HIS front desk, ensure they
/// exist in the EHR as a registered patient." Closes the synthetic-id gap that previously
/// caused HIS-originated patients to 404 on the EHR chart.
/// </summary>
/// <remarks>
/// Idempotent — a patient already present (by HIS-provided PatientId) is left alone.
/// New patients are created with the HIS-supplied name + MRN; demographics that HIS doesn't
/// carry on its event (date of birth, sex, language) are stubbed with sentinel values that
/// the receptionist / clinical staff can correct via a normal EHR update once the patient
/// is in the chair. The placeholder DOB is intentionally a long-ago date so it is obviously
/// not real data; the alternative — a near-today date — would look plausibly correct and
/// silently mislead.
/// </remarks>
public sealed class EhrPatientFromHisCheckInConsumer(
    IPatientRepository patients,
    IUnitOfWork unitOfWork,
    ILogger<EhrPatientFromHisCheckInConsumer> logger)
    : IConsumer<PatientCheckedInIntegrationEvent>
{
    private static readonly DateOnly PlaceholderDateOfBirth = new(1900, 1, 1);

    public async Task HandleAsync(ConsumeContext<PatientCheckedInIntegrationEvent> context)
    {
        var message = context.Message;
        var existing = await patients
            .GetAsync(message.PatientId, context.CancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var name = ParseName(message.PatientName);
        var patient = Patient.Register(
            id: message.PatientId,
            medicalRecordNumber: message.Mrn,
            name: name,
            dateOfBirth: PlaceholderDateOfBirth,
            sexAtBirthCode: null,
            preferredLanguageCode: null);
        patients.Add(patient);

        await unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Mirrored HIS-checked-in patient {PatientId} ({Mrn}) into EHR with placeholder demographics.",
            patient.Id,
            patient.MedicalRecordNumber);
    }

    /// <summary>
    /// Splits a full name on the last whitespace ("Anna Müller" → ("Müller", "Anna")).
    /// Single-token names fall back to using the same token for both parts so
    /// <see cref="HumanName"/>'s non-empty invariants are satisfied; clinical staff can
    /// correct the split later via a regular demographics update.
    /// </summary>
    private static HumanName ParseName(string fullName)
    {
        var trimmed = fullName.Trim();
        if (trimmed.Length == 0)
        {
            return new HumanName("Unknown", "Unknown");
        }

        var lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace <= 0)
        {
            return new HumanName(trimmed, trimmed);
        }

        var given = trimmed[..lastSpace].Trim();
        var family = trimmed[(lastSpace + 1)..].Trim();
        return new HumanName(
            familyName: family.Length == 0 ? trimmed : family,
            givenName: given.Length == 0 ? trimmed : given);
    }
}

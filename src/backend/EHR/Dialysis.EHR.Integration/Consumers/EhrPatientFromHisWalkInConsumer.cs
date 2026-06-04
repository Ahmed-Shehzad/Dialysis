using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>
/// Cross-module policy: "When an unannounced patient is registered as a walk-in at the
/// HIS front desk, ensure they exist in the EHR." Walk-ins skip the Expected → Waiting
/// transition (no prior appointment), so the check-in mirroring path doesn't catch them;
/// this consumer handles the parallel case.
/// </summary>
/// <remarks>
/// Same shape as <see cref="EhrPatientFromHisCheckInConsumer"/>: idempotent by HIS-supplied
/// PatientId, stubs demographics HIS doesn't carry (DOB / sex / language). Carried as a
/// separate consumer so the event semantics stay disjoint — walk-ins might later need
/// different handling (e.g. an explicit "from walk-in" provenance marker), and a single
/// consumer overloaded on both events would muddy that boundary.
/// </remarks>
public sealed class EhrPatientFromHisWalkInConsumer : IConsumer<WalkInRegisteredIntegrationEvent>
{
    private readonly IPatientRepository _patients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EhrPatientFromHisWalkInConsumer> _logger;
    /// <summary>
    /// Cross-module policy: "When an unannounced patient is registered as a walk-in at the
    /// HIS front desk, ensure they exist in the EHR." Walk-ins skip the Expected → Waiting
    /// transition (no prior appointment), so the check-in mirroring path doesn't catch them;
    /// this consumer handles the parallel case.
    /// </summary>
    /// <remarks>
    /// Same shape as <see cref="EhrPatientFromHisCheckInConsumer"/>: idempotent by HIS-supplied
    /// PatientId, stubs demographics HIS doesn't carry (DOB / sex / language). Carried as a
    /// separate consumer so the event semantics stay disjoint — walk-ins might later need
    /// different handling (e.g. an explicit "from walk-in" provenance marker), and a single
    /// consumer overloaded on both events would muddy that boundary.
    /// </remarks>
    public EhrPatientFromHisWalkInConsumer(IPatientRepository patients,
        IUnitOfWork unitOfWork,
        ILogger<EhrPatientFromHisWalkInConsumer> logger)
    {
        _patients = patients;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    private static readonly DateOnly _placeholderDateOfBirth = new(1900, 1, 1);

    public async Task HandleAsync(ConsumeContext<WalkInRegisteredIntegrationEvent> context)
    {
        var message = context.Message;
        var existing = await _patients
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
            dateOfBirth: _placeholderDateOfBirth,
            sexAtBirthCode: null,
            preferredLanguageCode: null);
        _patients.Add(patient);

        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Mirrored HIS walk-in patient {PatientId} ({Mrn}) into EHR with placeholder demographics.",
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

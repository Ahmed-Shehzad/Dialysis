using Bogus;
using Dialysis.Alerting.Features.ProcessAlerts;
using Dialysis.Contracts.Ids;
using Dialysis.AuditConsent.Features.Audit;
using Dialysis.Contracts.Events;
using Dialysis.DeviceIngestion.Features.IngestVitals;
using Dialysis.HisIntegration.Features.AdtSync;
using Dialysis.HisIntegration.Features.Hl7Streaming;
using Dialysis.IdentityAdmission.Features.PatientAdmission;
using Dialysis.IdentityAdmission.Features.SessionScheduling;

namespace Dialysis.TestUtilities;

public static class BogusFakers
{
    public static readonly Faker Faker = new();

    public static Faker<CreateAlertCommand> CreateAlertCommandFaker() => new Faker<CreateAlertCommand>()
        .RuleFor(x => x.PatientId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.EncounterId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.Code, f => f.PickRandom("HYPOTENSION_RISK", "CRITICAL_BP", "TACHYCARDIA"))
        .RuleFor(x => x.Severity, f => f.PickRandom("low", "medium", "high", "critical"))
        .RuleFor(x => x.Message, f => f.Lorem.Sentence());

    public static Faker<AcknowledgeAlertCommand> AcknowledgeAlertCommandFaker() => new Faker<AcknowledgeAlertCommand>()
        .RuleFor(x => x.AlertId, f => Ulid.NewUlid().ToString());

    public static Faker<RecordAuditCommand> RecordAuditCommandFaker() => new Faker<RecordAuditCommand>()
        .RuleFor(x => x.ResourceType, f => f.PickRandom("Patient", "Observation", "Encounter"))
        .RuleFor(x => x.ResourceId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.Action, f => f.PickRandom("create", "update", "read", "delete"))
        .RuleFor(x => x.AgentId, f => f.Internet.UserName())
        .RuleFor(x => x.Outcome, f => f.PickRandom("0", "4", null));

    public static Faker<IngestVitalsCommand> IngestVitalsCommandFaker() => new Faker<IngestVitalsCommand>()
        .RuleFor(x => x.PatientId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.EncounterId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.DeviceId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.Readings, f => VitalReadingFaker().GenerateBetween(1, 5));

    public static Faker<VitalReading> VitalReadingFaker() => new Faker<VitalReading>()
        .RuleFor(x => x.Code, f => f.PickRandom("8480-6", "8867-4", "59408-5", "8462-4"))
        .RuleFor(x => x.Value, f => f.Random.Double(60, 180).ToString("F1"))
        .RuleFor(x => x.Unit, f => f.PickRandom("mmHg", "/min", "%"))
        .RuleFor(x => x.Effective, f => f.Date.RecentOffset());

    public static Faker<AdmitPatientCommand> AdmitPatientCommandFaker() => new Faker<AdmitPatientCommand>()
        .RuleFor(x => x.Mrn, f => "MRN" + f.Random.AlphaNumeric(8))
        .RuleFor(x => x.FamilyName, f => f.Name.LastName())
        .RuleFor(x => x.GivenName, f => f.Name.FirstName())
        .RuleFor(x => x.BirthDate, f => f.Date.PastOffset(80));

    public static Faker<CreateSessionCommand> CreateSessionCommandFaker() => new Faker<CreateSessionCommand>()
        .RuleFor(x => x.PatientId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.DeviceId, f => f.Random.Guid().ToString())
        .RuleFor(x => x.ScheduledStart, f => f.Date.FutureOffset());

    public static Faker<ObservationCreated> ObservationCreatedFaker() => new Faker<ObservationCreated>()
        .CustomInstantiator(f => new ObservationCreated(
            Ulid.NewUlid(),
            f.PickRandom("default", "tenant1", null),
            ObservationId.Create(f.Random.Guid().ToString().Replace("-", "")[..24]),
            PatientId.Create(f.Random.Guid().ToString().Replace("-", "")[..24]),
            EncounterId.Create(f.Random.Guid().ToString().Replace("-", "")[..24]),
            f.PickRandom("8480-6", "8867-4", "59408-5"),
            f.Random.Double(60, 180).ToString("F1"),
            f.Date.RecentOffset(),
            f.Random.Guid().ToString()));

    public static Faker<Hl7StreamIngestCommand> Hl7StreamIngestCommandFaker() => new Faker<Hl7StreamIngestCommand>()
        .RuleFor(x => x.RawMessage, f => GenerateFakeAdtMessage(f))
        .RuleFor(x => x.MessageType, f => f.PickRandom("ADT_A01", "ADT_A02", "ADT_A03", "ADT_A08"))
        .RuleFor(x => x.TenantId, f => f.PickRandom("default", "tenant1", null));

    private static string GenerateFakeAdtMessage(Faker f)
    {
        var mrn = "MRN" + f.Random.AlphaNumeric(8);
        var family = f.Name.LastName();
        var given = f.Name.FirstName();
        var dob = f.Date.Past(80).ToString("yyyyMMdd");
        var msgType = f.PickRandom("ADT^A01", "ADT^A02", "ADT^A08");
        return $"MSH|^~\\&|HIS|HOSP|PDMS|CLINIC|{DateTime.Now:yyyyMMddHHmmss}||{msgType}|MSG{f.Random.Number(1000, 9999)}|P|2.5\r\n" +
               $"PID|1||{mrn}^^^HOSP^MR||{family}^{given}||{dob}|M\r\n" +
               $"PV1|1|I|^Ward^|||||^Smith^Jane|||ADM||";
    }
}

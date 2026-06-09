using System.Runtime.CompilerServices;
using Dialysis.DomainDrivenDesign.Specifications;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Dialysis.PDMS.Reporting.Directory;
using Dialysis.PDMS.Reporting.Generators;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Tests the PDMS-owned report-context builder that feeds the discharge / billing PDFs and the
/// billing charge. Two invariants: the duration it reports is the pause-aware machine usage time
/// (so reporting + invoice agree with the live estimate), and it enriches the context with the
/// session's medications and alarms.
/// </summary>
public sealed class SessionReportContextBuilderTests
{
    [Fact]
    public async Task Builds_Pause_Aware_Duration_Async()
    {
        var session = NewStartedSession(out var start);
        // Run 60, pause 20, run 60 → 120 min of machine usage.
        session.Pause(start.AddMinutes(60));
        session.Resume(start.AddMinutes(80));
        session.Complete(start.AddMinutes(140), achievedUfVolumeLiters: 2.5m);

        var builder = new SessionReportContextBuilder(
            new StubSessionRepo(session), new StubMedications(), new StubAlarms(), new StubPatientDirectory());
        var ctx = await builder.BuildAsync(session.Id, CancellationToken.None);

        ctx.ShouldNotBeNull();
        ctx!.DurationMinutes.ShouldBe(120);
        ctx.Modality.ShouldBe("HD");
        ctx.SessionId.ShouldBe(session.Id);
        ctx.PatientId.ShouldBe(session.PatientId);
    }

    [Fact]
    public async Task Enriches_Context_With_Medications_And_Alarms_Async()
    {
        var session = NewStartedSession(out var start);
        session.Complete(start.AddMinutes(120), achievedUfVolumeLiters: 2.0m);

        var mar = new MedicationAdministrationRecord(Guid.NewGuid(), session.Id, session.PatientId, start);
        mar.RecordAdministration(
            Guid.NewGuid(),
            new MedicationCoding("http://snomed.info/sct", "387467008", "Heparin"),
            new Dose(5000m, "U"),
            MedicationRoute.Intravenous,
            start.AddMinutes(5),
            administeredBySub: "nurse-1",
            relatedOrderId: null);

        var alarm = TreatmentAlarm.Raise(
            Guid.NewGuid(), session.Id, Guid.NewGuid(), alarmCode: 42,
            alarmSource: "Venous pressure high", alarmPhase: "Dialysis", observedAtUtc: start.AddMinutes(10));

        var builder = new SessionReportContextBuilder(
            new StubSessionRepo(session), new StubMedications(mar), new StubAlarms(alarm), new StubPatientDirectory());
        var ctx = await builder.BuildAsync(session.Id, CancellationToken.None);

        ctx.ShouldNotBeNull();
        var med = ctx!.Medications.ShouldHaveSingleItem();
        med.MedicationDisplay.ShouldBe("Heparin");
        med.DoseQuantity.ShouldBe(5000m);
        med.WasAdministered.ShouldBeTrue();

        var raised = ctx.Alarms.ShouldHaveSingleItem();
        raised.AlarmCode.ShouldBe("42");
        raised.AlarmText.ShouldBe("Venous pressure high");
        raised.Acknowledged.ShouldBeFalse();
    }

    [Fact]
    public async Task Returns_Null_For_Unknown_Session_Async()
    {
        var builder = new SessionReportContextBuilder(
            new StubSessionRepo(null), new StubMedications(), new StubAlarms(), new StubPatientDirectory());
        (await builder.BuildAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Fills_Real_Name_And_Mrn_From_The_Patient_Directory_Async()
    {
        var session = NewStartedSession(out var start);
        session.Complete(start.AddMinutes(120), achievedUfVolumeLiters: 2.0m);

        var directoryEntry = PatientDirectoryEntry.From(
            session.PatientId, "MRN-99001", "Marcus", "Bell", new DateOnly(1968, 3, 14), start);

        var builder = new SessionReportContextBuilder(
            new StubSessionRepo(session), new StubMedications(), new StubAlarms(),
            new StubPatientDirectory(directoryEntry));
        var ctx = await builder.BuildAsync(session.Id, CancellationToken.None);

        ctx.ShouldNotBeNull();
        ctx!.PatientDisplayName.ShouldBe("Marcus Bell");
        ctx.MedicalRecordNumber.ShouldBe("MRN-99001");
    }

    [Fact]
    public async Task Falls_Back_To_Placeholder_When_Patient_Not_In_Directory_Async()
    {
        var session = NewStartedSession(out var start);
        session.Complete(start.AddMinutes(120), achievedUfVolumeLiters: 2.0m);

        var builder = new SessionReportContextBuilder(
            new StubSessionRepo(session), new StubMedications(), new StubAlarms(), new StubPatientDirectory());
        var ctx = await builder.BuildAsync(session.Id, CancellationToken.None);

        ctx.ShouldNotBeNull();
        ctx!.PatientDisplayName.ShouldStartWith("Patient ");
    }

    private static DialysisSession NewStartedSession(out DateTime start)
    {
        var session = DialysisSession.Schedule(
            id: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
            scheduledStartUtc: DateTime.UtcNow.AddMinutes(5),
            prescription: new SessionPrescription(
                dialyzerModel: "Polyflux 17L",
                prescribedDurationMinutes: 240,
                bloodFlowRateMlPerMin: 350,
                dialysateFlowRateMlPerMin: 500,
                dialysatePotassiumMmolPerL: 2.0m,
                dialysateCalciumMmolPerL: 1.25m,
                dialysateSodiumMmolPerL: 140m,
                targetUfVolumeLiters: 2.5m,
                anticoagulationProtocolCode: "heparin-bolus"),
            access: new VascularAccess(
                VascularAccessKind.ArteriovenousFistula,
                "Left forearm",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1))));
        start = DateTime.UtcNow;
        session.Start(start);
        return session;
    }

    private sealed class StubSessionRepo : IDialysisSessionRepository
    {
        private readonly DialysisSession? _session;
        public StubSessionRepo(DialysisSession? session) => _session = session;
        public Task<DialysisSession?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_session is not null && _session.Id == id ? _session : null);
        public Task<IReadOnlyList<DialysisSession>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DialysisSession>>([]);
        public Task<IReadOnlyList<DialysisSession>> ListRecentAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DialysisSession>>([]);
        public Task<IReadOnlyList<DialysisSession>> ListActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DialysisSession>>([]);
        public void Add(DialysisSession session) { }
        public async IAsyncEnumerable<DialysisSession> StreamAllAsync(DateTimeOffset? since, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class StubMedications : IPdmsRepository<MedicationAdministrationRecord, Guid>
    {
        private readonly List<MedicationAdministrationRecord> _records;
        public StubMedications(params MedicationAdministrationRecord[] seed) => _records = [.. seed];
        public Task<MedicationAdministrationRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records.FirstOrDefault(r => r.Id == id));
        public Task<IReadOnlyList<MedicationAdministrationRecord>> ListAsync(
            ISpecification<MedicationAdministrationRecord>? specification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MedicationAdministrationRecord>>([.. _records]);
        public Task AddAsync(MedicationAdministrationRecord aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(MedicationAdministrationRecord aggregate) { }
        public void Remove(MedicationAdministrationRecord aggregate) { }
    }

    private sealed class StubPatientDirectory : IPdmsRepository<PatientDirectoryEntry, Guid>
    {
        private readonly List<PatientDirectoryEntry> _entries;
        public StubPatientDirectory(params PatientDirectoryEntry[] seed) => _entries = [.. seed];
        public Task<PatientDirectoryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));
        public Task<IReadOnlyList<PatientDirectoryEntry>> ListAsync(
            ISpecification<PatientDirectoryEntry>? specification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PatientDirectoryEntry>>([.. _entries]);
        public Task AddAsync(PatientDirectoryEntry aggregate, CancellationToken cancellationToken = default)
        {
            _entries.Add(aggregate);
            return Task.CompletedTask;
        }
        public void Update(PatientDirectoryEntry aggregate) { }
        public void Remove(PatientDirectoryEntry aggregate) => _entries.Remove(aggregate);
    }

    private sealed class StubAlarms : ITreatmentAlarmRepository
    {
        private readonly List<TreatmentAlarm> _alarms;
        public StubAlarms(params TreatmentAlarm[] seed) => _alarms = [.. seed];
        public void Add(TreatmentAlarm alarm) => _alarms.Add(alarm);
        public Task<TreatmentAlarm?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_alarms.FirstOrDefault(a => a.Id == id));
        public Task<TreatmentAlarm?> FindLiveAsync(Guid machineId, long alarmCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<TreatmentAlarm?>(null);
        public Task<IReadOnlyList<TreatmentAlarm>> ListActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TreatmentAlarm>>([.. _alarms]);
        public Task<IReadOnlyList<TreatmentAlarm>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TreatmentAlarm>>([.. _alarms.Where(a => a.SessionId == sessionId)]);
    }
}

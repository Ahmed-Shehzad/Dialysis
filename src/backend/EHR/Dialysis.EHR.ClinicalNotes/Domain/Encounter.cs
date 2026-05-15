using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.ClinicalNotes.Domain;

public sealed class Encounter : AggregateRoot<Guid>
{
    private readonly List<Diagnosis> _diagnoses = new();
    private readonly List<PerformedProcedure> _procedures = new();

    private Encounter()
    {
    }

    public Encounter(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid ProviderId { get; private set; }

    public Guid? AppointmentId { get; private set; }

    public string EncounterClassCode { get; private set; } = string.Empty;

    public EncounterStatus Status { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<Diagnosis> Diagnoses => _diagnoses;

    public IReadOnlyCollection<PerformedProcedure> Procedures => _procedures;

    public static Encounter Open(
        Guid id,
        Guid patientId,
        Guid providerId,
        string encounterClassCode,
        DateTime startedAtUtc,
        Guid? appointmentId = null)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider required.", nameof(providerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(encounterClassCode);

        var encounter = new Encounter(id)
        {
            PatientId = patientId,
            ProviderId = providerId,
            AppointmentId = appointmentId,
            EncounterClassCode = encounterClassCode.Trim(),
            Status = EncounterStatus.InProgress,
            StartedAtUtc = startedAtUtc,
        };

        encounter.RaiseIntegrationEvent(new EncounterOpenedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            EncounterId: id,
            PatientId: patientId,
            ProviderId: providerId,
            EncounterClassCode: encounter.EncounterClassCode,
            StartedAtUtc: startedAtUtc));

        return encounter;
    }

    public Diagnosis AttachDiagnosis(string icd10Code, DiagnosisRank rank, string? display, DateTime recordedAtUtc)
    {
        EnsureMutable();
        if (rank == DiagnosisRank.Primary && _diagnoses.Any(d => d.Rank == DiagnosisRank.Primary))
            throw new InvalidOperationException("Encounter already has a primary diagnosis.");
        var diagnosis = Diagnosis.Record(Guid.CreateVersion7(), Id, icd10Code, rank, display, recordedAtUtc);
        _diagnoses.Add(diagnosis);
        return diagnosis;
    }

    public PerformedProcedure AttachProcedure(string cptCode, DateTime performedAtUtc, Guid performingProviderId, IReadOnlyList<string>? modifiers, string? display)
    {
        EnsureMutable();
        var procedure = PerformedProcedure.Record(Guid.CreateVersion7(), Id, cptCode, performedAtUtc, performingProviderId, modifiers, display);
        _procedures.Add(procedure);
        return procedure;
    }

    public void Close(DateTime closedAtUtc)
    {
        if (Status == EncounterStatus.Finished)
            return;
        if (Status == EncounterStatus.Cancelled)
            throw new InvalidOperationException("Cannot close a cancelled encounter.");
        if (_diagnoses.Count == 0)
            throw new InvalidOperationException("Cannot close an encounter without at least one diagnosis.");

        Status = EncounterStatus.Finished;
        ClosedAtUtc = closedAtUtc;

        RaiseIntegrationEvent(new EncounterClosedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            EncounterId: Id,
            PatientId: PatientId,
            ProviderId: ProviderId,
            ClosedAtUtc: closedAtUtc,
            DiagnosisIcd10Codes: [.. _diagnoses.Select(d => d.Icd10Code)],
            ProcedureCptCodes: [.. _procedures.Select(p => p.CptCode)]));
    }

    public void Cancel()
    {
        if (Status == EncounterStatus.Finished)
            throw new InvalidOperationException("Cannot cancel a finished encounter.");
        Status = EncounterStatus.Cancelled;
    }

    private void EnsureMutable()
    {
        if (Status is EncounterStatus.Finished or EncounterStatus.Cancelled)
            throw new InvalidOperationException($"Cannot modify a {Status} encounter.");
    }
}

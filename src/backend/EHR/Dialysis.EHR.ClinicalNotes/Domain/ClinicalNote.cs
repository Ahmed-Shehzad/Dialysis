using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.ClinicalNotes.Domain;

public enum ClinicalNoteStatus
{
    Draft = 1,
    Signed = 2,
    Amended = 3,
    EnteredInError = 4,
}

/// <summary>SOAP-structured encounter note. Aggregate keyed independently so multiple notes per encounter are allowed.</summary>
public sealed class ClinicalNote : AggregateRoot<Guid>
{
    private ClinicalNote()
    {
    }

    public ClinicalNote(Guid id) : base(id)
    {
    }

    public Guid EncounterId { get; private set; }

    public Guid PatientId { get; private set; }

    public Guid AuthoringProviderId { get; private set; }

    public string Subjective { get; private set; } = string.Empty;

    public string Objective { get; private set; } = string.Empty;

    public string Assessment { get; private set; } = string.Empty;

    public string Plan { get; private set; } = string.Empty;

    public ClinicalNoteStatus Status { get; private set; }

    public Guid? SignedByProviderId { get; private set; }

    public DateTime? SignedAtUtc { get; private set; }

    public static ClinicalNote Draft(
        Guid id,
        Guid encounterId,
        Guid patientId,
        Guid authoringProviderId,
        string subjective,
        string objective,
        string assessment,
        string plan)
    {
        if (encounterId == Guid.Empty) throw new ArgumentException("Encounter required.", nameof(encounterId));
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        if (authoringProviderId == Guid.Empty) throw new ArgumentException("Author required.", nameof(authoringProviderId));

        return new ClinicalNote(id)
        {
            EncounterId = encounterId,
            PatientId = patientId,
            AuthoringProviderId = authoringProviderId,
            Subjective = subjective ?? string.Empty,
            Objective = objective ?? string.Empty,
            Assessment = assessment ?? string.Empty,
            Plan = plan ?? string.Empty,
            Status = ClinicalNoteStatus.Draft,
        };
    }

    public void Update(string subjective, string objective, string assessment, string plan)
    {
        if (Status != ClinicalNoteStatus.Draft)
            throw new InvalidOperationException("Only draft notes are mutable. Use Amend instead.");
        Subjective = subjective ?? string.Empty;
        Objective = objective ?? string.Empty;
        Assessment = assessment ?? string.Empty;
        Plan = plan ?? string.Empty;
    }

    public void Sign(Guid signingProviderId, DateTime signedAtUtc)
    {
        if (signingProviderId == Guid.Empty) throw new ArgumentException("Signer required.", nameof(signingProviderId));
        if (Status == ClinicalNoteStatus.Signed) return;
        if (Status == ClinicalNoteStatus.EnteredInError)
            throw new InvalidOperationException("Cannot sign a note marked as entered-in-error.");
        if (string.IsNullOrWhiteSpace(Assessment))
            throw new InvalidOperationException("Cannot sign a note without an assessment.");

        Status = ClinicalNoteStatus.Signed;
        SignedByProviderId = signingProviderId;
        SignedAtUtc = signedAtUtc;

        RaiseIntegrationEvent(new ClinicalNoteSignedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            NoteId: Id,
            EncounterId: EncounterId,
            PatientId: PatientId,
            SignedByProviderId: signingProviderId,
            SignedAtUtc: signedAtUtc));
    }

    public void Amend(string subjective, string objective, string assessment, string plan)
    {
        if (Status != ClinicalNoteStatus.Signed)
            throw new InvalidOperationException("Only signed notes can be amended.");
        Status = ClinicalNoteStatus.Amended;
        Subjective = subjective ?? string.Empty;
        Objective = objective ?? string.Empty;
        Assessment = assessment ?? string.Empty;
        Plan = plan ?? string.Empty;
    }
}

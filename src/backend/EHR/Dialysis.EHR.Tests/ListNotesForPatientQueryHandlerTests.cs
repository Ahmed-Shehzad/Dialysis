using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.ListNotesForPatient;
using Dialysis.EHR.ClinicalNotes.Ports;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class ListNotesForPatientQueryHandlerTests
{
    [Fact]
    public async Task Returns_Patient_Notes_Filtered_And_Mapped_Async()
    {
        var patient = Guid.NewGuid();
        var encounter = Guid.NewGuid();
        var provider = Guid.NewGuid();

        var mine = ClinicalNote.Draft(
            id: Guid.NewGuid(),
            encounterId: encounter,
            patientId: patient,
            authoringProviderId: provider,
            subjective: "BP elevated",
            objective: "",
            assessment: "Hypertensive urgency",
            plan: "Recheck in 30m");
        var someoneElse = ClinicalNote.Draft(
            id: Guid.NewGuid(),
            encounterId: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
            authoringProviderId: provider,
            subjective: "Cough",
            objective: "",
            assessment: "URI",
            plan: "Hydrate");

        var handler = new ListNotesForPatientQueryHandler(new InMemoryNotes(mine, someoneElse));

        var result = await handler.HandleAsync(
            new ListNotesForPatientQuery(patient, Take: 20),
            CancellationToken.None);

        result.Count.ShouldBe(1, "Other patients' notes are filtered out by the repository fake.");
        result[0].Id.ShouldBe(mine.Id);
        result[0].Assessment.ShouldBe("Hypertensive urgency");
        result[0].Status.ShouldBe((int)ClinicalNoteStatus.Draft);
    }

    [Fact]
    public async Task Honours_Take_Clamp_Async()
    {
        var patient = Guid.NewGuid();
        var notes = Enumerable.Range(0, 5)
            .Select(_ => ClinicalNote.Draft(
                id: Guid.NewGuid(),
                encounterId: Guid.NewGuid(),
                patientId: patient,
                authoringProviderId: Guid.NewGuid(),
                subjective: "",
                objective: "",
                assessment: "ok",
                plan: ""))
            .ToArray();
        var handler = new ListNotesForPatientQueryHandler(new InMemoryNotes(notes));

        var result = await handler.HandleAsync(
            new ListNotesForPatientQuery(patient, Take: 2),
            CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    private sealed class InMemoryNotes : IClinicalNoteRepository
    {
        private readonly IReadOnlyList<ClinicalNote> _notes;
        public InMemoryNotes(params ClinicalNote[] seed) => _notes = [.. seed];

        public Task<ClinicalNote?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<ClinicalNote>> ListByEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ClinicalNote>>([.. _notes.Where(n => n.EncounterId == encounterId)]);

        public Task<IReadOnlyList<ClinicalNote>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ClinicalNote>>(
                [.. _notes.Where(n => n.PatientId == patientId).Take(take)]);

        public void Add(ClinicalNote note) => throw new NotSupportedException();
    }

}

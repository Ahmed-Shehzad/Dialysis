using System.Runtime.CompilerServices;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.SafetyChecks;

/// <summary>In-memory test doubles + builders for the point-of-care safety checks.</summary>
internal static class SafetyTestData
{
    public static Allergy Allergy(Guid patientId, string code, string display,
        AllergyVerificationStatus status = AllergyVerificationStatus.Confirmed) =>
        EHR.PatientChart.Domain.Allergy.Record(
            Guid.NewGuid(), patientId, new Coding("http://snomed.info/sct", code, display),
            AllergySeverity.Severe, status);

    public static MedicationStatement Medication(Guid patientId, string code, string display) =>
        MedicationStatement.Record(
            Guid.NewGuid(), patientId, new Coding("http://www.nlm.nih.gov/research/umls/rxnorm", code, display),
            "1 tab", "daily", DateOnly.FromDateTime(DateTime.UtcNow));

    public static Prescription Prescription(Guid patientId, string rxnorm, string display) =>
        EHR.ClinicalNotes.Domain.Prescription.Order(
            Guid.NewGuid(), patientId, Guid.NewGuid(), Guid.NewGuid(),
            rxnorm, display, "1 tab", "daily", 30, 0, "PHARM-1", "ncpdp");

    public static LabOrder LabOrder(Guid patientId, params string[] loinc) =>
        EHR.ClinicalNotes.Domain.LabOrder.Order(
            Guid.NewGuid(), patientId, Guid.NewGuid(), Guid.NewGuid(), "LAB-1", loinc, "fhir");
}

internal sealed class FakeAllergyRepository : IAllergyRepository
{
    private readonly List<Allergy> _items;
    public FakeAllergyRepository(params Allergy[] items) => _items = [.. items];
    public Task<Allergy?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(a => a.Id == id));
    public Task<IReadOnlyList<Allergy>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Allergy>>([.. _items.Where(a => a.PatientId == patientId)]);
    public async IAsyncEnumerable<Allergy> StreamAllAsync(DateTimeOffset? since,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var a in _items)
        { yield return a; }
        await Task.CompletedTask.ConfigureAwait(false);
    }
    public void Add(Allergy allergy) => _items.Add(allergy);
}

internal sealed class FakeMedicationStatementRepository : IMedicationStatementRepository
{
    private readonly List<MedicationStatement> _items;
    public FakeMedicationStatementRepository(params MedicationStatement[] items) => _items = [.. items];
    public Task<MedicationStatement?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(m => m.Id == id));
    public Task<IReadOnlyList<MedicationStatement>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MedicationStatement>>(
            [.. _items.Where(m => m.PatientId == patientId
                && (!activeOnly || m.Status == MedicationStatementStatus.Active))]);
    public async IAsyncEnumerable<MedicationStatement> StreamAllAsync(DateTimeOffset? since,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var m in _items)
        { yield return m; }
        await Task.CompletedTask.ConfigureAwait(false);
    }
    public void Add(MedicationStatement statement) => _items.Add(statement);
}

internal sealed class FakePrescriptionRepository : IPrescriptionRepository
{
    private readonly List<Prescription> _items;
    public FakePrescriptionRepository(params Prescription[] items) => _items = [.. items];
    public Prescription? Added { get; private set; }
    public Task<Prescription?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(p => p.Id == id));
    public Task<IReadOnlyList<Prescription>> ListActiveByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Prescription>>(
            [.. _items.Where(p => p.PatientId == patientId && p.Status == PrescriptionStatus.Active)]);
    public void Add(Prescription prescription)
    {
        Added = prescription;
        _items.Add(prescription);
    }
}

internal sealed class FakeLabOrderRepository : ILabOrderRepository
{
    private readonly List<(LabOrder Order, DateTime CreatedAt)> _items;
    public FakeLabOrderRepository(params (LabOrder Order, DateTime CreatedAt)[] items) => _items = [.. items];
    public LabOrder? Added { get; private set; }
    public Task<LabOrder?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Select(i => i.Order).FirstOrDefault(o => o.Id == id));

    // Simulates the EF repo's window + non-cancelled filter using the seeded created-at timestamps.
    public Task<IReadOnlyList<LabOrder>> ListRecentByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LabOrder>>(
            [.. _items.Where(i => i.Order.PatientId == patientId
                && i.Order.Status != LabOrderStatus.Cancelled
                && i.CreatedAt >= sinceUtc).Select(i => i.Order)]);

    public void Add(LabOrder labOrder)
    {
        Added = labOrder;
        _items.Add((labOrder, DateTime.UtcNow));
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeSafetyChecker : IClinicalSafetyChecker
{
    private readonly SafetyAdvisoryResult _result;
    public FakeSafetyChecker(SafetyAdvisoryResult result) => _result = result;
    public Task<SafetyAdvisoryResult> CheckPrescriptionAsync(Guid patientId, string medicationRxnormCode, string medicationDisplay, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
    public Task<SafetyAdvisoryResult> CheckLabOrderAsync(Guid patientId, IReadOnlyList<string> loincPanelCodes, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
}

internal sealed class FixedClock : TimeProvider
{
    private readonly DateTime _utcNow;
    public FixedClock(DateTime utcNow) => _utcNow = utcNow;
    public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
}

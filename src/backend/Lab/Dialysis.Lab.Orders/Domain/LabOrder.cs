using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.IntegrationEvents;

namespace Dialysis.Lab.Orders.Domain;

/// <summary>One requested test on an order (LOINC-coded).</summary>
public sealed record LabTestItem(string LoincCode, string Display);

/// <summary>One resulted observation on an order.</summary>
public sealed record LabResultItem(
    string LoincCode,
    string Display,
    string Value,
    string? Unit,
    string? ReferenceRange,
    LabResultInterpretation Interpretation);

/// <summary>
/// Aggregate root for a laboratory order. Owns the order's lifecycle (placed → transmitted → in
/// progress → resulted, or cancelled), the requested tests, the returned results, and the
/// placer/filler order-number pair used to match an inbound result back to the order.
/// Lab is a headless bounded context: order-entry lives in the EHR app (via the EHR BFF), the wire
/// transport (HL7v2/FHIR) lives in SmartConnect — this aggregate only owns the order state.
/// </summary>
public sealed class LabOrder : AggregateRoot<Guid>
{
    private readonly List<LabTestItem> _tests = [];
    private readonly List<LabResultItem> _results = [];

    public Guid PatientId { get; private set; }
    public string PlacerOrderNumber { get; private set; } = null!;
    public string? FillerOrderNumber { get; private set; }
    public LabOrderPriority Priority { get; private set; }
    public string? Specimen { get; private set; }
    public LabOrderStatus Status { get; private set; } = LabOrderStatus.Placed;
    public string PlacedBy { get; private set; } = null!;
    public DateTime PlacedAtUtc { get; private set; }
    public DateTime? ResultedAtUtc { get; private set; }

    public IReadOnlyCollection<LabTestItem> Tests => _tests;
    public IReadOnlyCollection<LabResultItem> Results => _results;

    private LabOrder()
    {
    }

    private LabOrder(Guid id) : base(id)
    {
    }

    /// <summary>Places a new order and raises <see cref="LabOrderPlacedIntegrationEvent"/> for transmission.</summary>
    public static LabOrder Place(
        Guid patientId,
        IReadOnlyList<LabTestItem> tests,
        LabOrderPriority priority,
        string? specimen,
        string placedBy,
        DateTime nowUtc)
    {
        if (patientId == Guid.Empty)
            throw new DomainException("LabOrder requires a patient.");
        ArgumentNullException.ThrowIfNull(tests);
        if (tests.Count == 0)
            throw new DomainException("LabOrder must request at least one test.");
        if (string.IsNullOrWhiteSpace(placedBy))
            throw new DomainException("LabOrder requires the placing clinician.");

        var id = Guid.CreateVersion7();
        // PlacerOrderNumber is unique-indexed (UX_LabOrders_PlacerOrderNumber) and capped at 32
        // chars. A v7 GUID's first 12 hex chars are only the 48-bit millisecond timestamp — two
        // orders placed in the same millisecond would collide — so pair the time-sortable prefix
        // with the GUID's 64-bit random tail: "LAB-" + 12 timestamp + 16 random = 32 chars.
        var hex = id.ToString("N").ToUpperInvariant();
        var order = new LabOrder(id)
        {
            PatientId = patientId,
            PlacerOrderNumber = "LAB-" + hex[..12] + hex[^16..],
            Priority = priority,
            Specimen = string.IsNullOrWhiteSpace(specimen) ? null : specimen.Trim(),
            Status = LabOrderStatus.Placed,
            PlacedBy = placedBy.Trim(),
            PlacedAtUtc = nowUtc,
        };
        order._tests.AddRange(tests);

        order.RaiseIntegrationEvent(new LabOrderPlacedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: nowUtc,
            SchemaVersion: 1,
            OrderId: order.Id,
            PatientId: patientId,
            PlacerOrderNumber: order.PlacerOrderNumber,
            Priority: priority,
            Specimen: order.Specimen,
            Tests: [.. tests.Select(t => new LabTestRequestContract(t.LoincCode, t.Display))],
            PlacedAtUtc: nowUtc));

        return order;
    }

    /// <summary>Marks the order transmitted to the LIS once SmartConnect dispatches it (optionally with the filler id).</summary>
    public void MarkTransmitted(string? fillerOrderNumber)
    {
        if (Status != LabOrderStatus.Placed)
            throw new DomainException($"Only a Placed order can be transmitted (was {Status}).");
        Status = LabOrderStatus.Transmitted;
        if (!string.IsNullOrWhiteSpace(fillerOrderNumber))
            FillerOrderNumber = fillerOrderNumber.Trim();
    }

    /// <summary>Records the returned observations and completes the order.</summary>
    public void RecordResults(IReadOnlyList<LabResultItem> results, string? fillerOrderNumber, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (Status == LabOrderStatus.Cancelled)
            throw new DomainException("Cannot result a cancelled order.");
        if (!string.IsNullOrWhiteSpace(fillerOrderNumber))
            FillerOrderNumber = fillerOrderNumber.Trim();
        _results.Clear();
        _results.AddRange(results);
        Status = LabOrderStatus.Resulted;
        ResultedAtUtc = nowUtc;
    }

    /// <summary>Cancels the order before it is resulted.</summary>
    public void Cancel()
    {
        if (Status == LabOrderStatus.Resulted)
            throw new DomainException("Cannot cancel an order that has already resulted.");
        Status = LabOrderStatus.Cancelled;
    }
}

using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.ClinicalNotes.Domain;

/// <summary>Which kind of order an <see cref="OrderSetLine"/> produces when the set is applied.</summary>
public enum OrderSetLineKind
{
    Lab = 1,
    Medication = 2,
    Imaging = 3,
}

/// <summary>One line of an <see cref="OrderSet"/> — the template payload for a single order.</summary>
public sealed class OrderSetLine : Entity<Guid>
{
    private readonly List<string> _loincPanelCodes = new();

    private OrderSetLine()
    {
    }

    private OrderSetLine(Guid id, Guid orderSetId, OrderSetLineKind kind) : base(id)
    {
        OrderSetId = orderSetId;
        Kind = kind;
    }

    public Guid OrderSetId { get; private set; }

    public OrderSetLineKind Kind { get; private set; }

    // Lab
    public string? LabFacilityCode { get; private set; }
    public IReadOnlyCollection<string> LoincPanelCodes => _loincPanelCodes;

    // Medication
    public string? MedicationRxnormCode { get; private set; }
    public string? MedicationDisplay { get; private set; }
    public string? DoseText { get; private set; }
    public string? FrequencyText { get; private set; }
    public int? QuantityDispensed { get; private set; }
    public int? RefillsAuthorized { get; private set; }
    public string? PharmacyNcpdpId { get; private set; }

    // Imaging
    public string? ModalityCode { get; private set; }
    public string? BodySiteCode { get; private set; }
    public string? ReasonText { get; private set; }

    internal static OrderSetLine Lab(Guid id, Guid orderSetId, string labFacilityCode, IReadOnlyList<string> loincPanelCodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(labFacilityCode);
        if (loincPanelCodes is null || loincPanelCodes.Count == 0)
            throw new ArgumentException("At least one LOINC panel is required.", nameof(loincPanelCodes));
        var line = new OrderSetLine(id, orderSetId, OrderSetLineKind.Lab) { LabFacilityCode = labFacilityCode.Trim() };
        line._loincPanelCodes.AddRange(loincPanelCodes.Select(c => c.Trim()).Where(static c => c.Length > 0));
        return line;
    }

    internal static OrderSetLine Medication(Guid id, Guid orderSetId, string rxnorm, string display,
        string doseText, string frequencyText, int quantityDispensed, int refillsAuthorized, string pharmacyNcpdpId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rxnorm);
        ArgumentException.ThrowIfNullOrWhiteSpace(display);
        ArgumentException.ThrowIfNullOrWhiteSpace(doseText);
        ArgumentException.ThrowIfNullOrWhiteSpace(frequencyText);
        ArgumentException.ThrowIfNullOrWhiteSpace(pharmacyNcpdpId);
        if (quantityDispensed <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityDispensed));
        if (refillsAuthorized < 0)
            throw new ArgumentOutOfRangeException(nameof(refillsAuthorized));
        return new OrderSetLine(id, orderSetId, OrderSetLineKind.Medication)
        {
            MedicationRxnormCode = rxnorm.Trim(),
            MedicationDisplay = display.Trim(),
            DoseText = doseText.Trim(),
            FrequencyText = frequencyText.Trim(),
            QuantityDispensed = quantityDispensed,
            RefillsAuthorized = refillsAuthorized,
            PharmacyNcpdpId = pharmacyNcpdpId.Trim(),
        };
    }

    internal static OrderSetLine Imaging(Guid id, Guid orderSetId, string modalityCode, string bodySiteCode, string? reasonText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modalityCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodySiteCode);
        return new OrderSetLine(id, orderSetId, OrderSetLineKind.Imaging)
        {
            ModalityCode = modalityCode.Trim(),
            BodySiteCode = bodySiteCode.Trim(),
            ReasonText = string.IsNullOrWhiteSpace(reasonText) ? null : reasonText.Trim(),
        };
    }
}

/// <summary>
/// A named, reusable bundle of orders (lab panels + medications + imaging) authored once and applied to a
/// patient in one action — "standardization of order sets / evidence-based medicine." Applying a set fans
/// out to the individual order commands, so each line still runs the point-of-care safety checks.
/// </summary>
public sealed class OrderSet : AggregateRoot<Guid>
{
    private readonly List<OrderSetLine> _lines = new();

    private OrderSet()
    {
    }

    public OrderSet(Guid id) : base(id)
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderSetLine> Lines => _lines;

    public static OrderSet Create(Guid id, string name, string? description, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new OrderSet(id)
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = true,
            CreatedAtUtc = nowUtc,
        };
    }

    public OrderSetLine AddLabLine(Guid lineId, string labFacilityCode, IReadOnlyList<string> loincPanelCodes)
    {
        var line = OrderSetLine.Lab(lineId, Id, labFacilityCode, loincPanelCodes);
        _lines.Add(line);
        return line;
    }

    public OrderSetLine AddMedicationLine(Guid lineId, string rxnorm, string display,
        string doseText, string frequencyText, int quantityDispensed, int refillsAuthorized, string pharmacyNcpdpId)
    {
        var line = OrderSetLine.Medication(lineId, Id, rxnorm, display, doseText, frequencyText, quantityDispensed, refillsAuthorized, pharmacyNcpdpId);
        _lines.Add(line);
        return line;
    }

    public OrderSetLine AddImagingLine(Guid lineId, string modalityCode, string bodySiteCode, string? reasonText)
    {
        var line = OrderSetLine.Imaging(lineId, Id, modalityCode, bodySiteCode, reasonText);
        _lines.Add(line);
        return line;
    }

    public void Deactivate() => IsActive = false;
}

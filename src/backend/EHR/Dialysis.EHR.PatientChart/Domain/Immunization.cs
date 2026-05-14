using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.PatientChart.Domain;

public enum ImmunizationStatus
{
    Completed = 1,
    EnteredInError = 2,
    NotDone = 3,
}

/// <summary>Vaccination event (CVX-coded vaccine, manufacturer, lot, administration site).</summary>
public sealed class Immunization : AggregateRoot<Guid>
{
    private Immunization()
    {
    }

    public Immunization(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Coding Vaccine { get; private set; } = null!;

    public DateOnly AdministeredOn { get; private set; }

    public string? LotNumber { get; private set; }

    public string? Manufacturer { get; private set; }

    public string? SiteCode { get; private set; }

    public ImmunizationStatus Status { get; private set; }

    public Guid? AdministeringProviderId { get; private set; }

    public static Immunization Record(
        Guid id,
        Guid patientId,
        Coding vaccine,
        DateOnly administeredOn,
        string? lotNumber = null,
        string? manufacturer = null,
        string? siteCode = null,
        Guid? administeringProviderId = null)
    {
        ArgumentNullException.ThrowIfNull(vaccine);
        if (patientId == Guid.Empty) throw new ArgumentException("Patient id required.", nameof(patientId));

        return new Immunization(id)
        {
            PatientId = patientId,
            Vaccine = vaccine,
            AdministeredOn = administeredOn,
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(),
            Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer.Trim(),
            SiteCode = string.IsNullOrWhiteSpace(siteCode) ? null : siteCode.Trim(),
            AdministeringProviderId = administeringProviderId,
            Status = ImmunizationStatus.Completed,
        };
    }

    public void MarkEnteredInError()
    {
        Status = ImmunizationStatus.EnteredInError;
    }
}

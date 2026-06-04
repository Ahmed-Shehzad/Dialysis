using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Scheduling.Domain;

/// <summary>Recurring or one-off availability window during which a provider accepts appointments at a location.</summary>
public sealed class ProviderAvailabilityWindow : AggregateRoot<Guid>
{
    private ProviderAvailabilityWindow()
    {
    }

    public ProviderAvailabilityWindow(Guid id) : base(id)
    {
    }

    public Guid ProviderId { get; private set; }

    public Guid? LocationId { get; private set; }

    public DateTime StartUtc { get; private set; }

    public DateTime EndUtc { get; private set; }

    public int SlotDurationMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public static ProviderAvailabilityWindow Open(
        Guid id,
        Guid providerId,
        DateTime startUtc,
        DateTime endUtc,
        int slotDurationMinutes,
        Guid? locationId = null)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider required.", nameof(providerId));
        if (endUtc <= startUtc)
            throw new ArgumentException("End must follow start.", nameof(endUtc));
        if (slotDurationMinutes is < 5 or > 240)
            throw new ArgumentOutOfRangeException(nameof(slotDurationMinutes), "Slot duration must be between 5 and 240 minutes.");

        return new ProviderAvailabilityWindow(id)
        {
            ProviderId = providerId,
            LocationId = locationId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            SlotDurationMinutes = slotDurationMinutes,
            IsActive = true,
        };
    }

    public void Close() => IsActive = false;
}

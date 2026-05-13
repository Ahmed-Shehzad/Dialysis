using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Billing.Domain;

public sealed class Payer : AggregateRoot<Guid>
{
    private Payer()
    {
    }

    public Payer(Guid id) : base(id)
    {
    }

    public string PayerCode { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? ClearinghouseCode { get; private set; }

    public bool IsActive { get; private set; }

    public static Payer Register(Guid id, string payerCode, string displayName, string? clearinghouseCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new Payer(id)
        {
            PayerCode = payerCode.Trim().ToUpperInvariant(),
            DisplayName = displayName.Trim(),
            ClearinghouseCode = string.IsNullOrWhiteSpace(clearinghouseCode) ? null : clearinghouseCode.Trim(),
            IsActive = true,
        };
    }

    public void Deactivate() => IsActive = false;
}

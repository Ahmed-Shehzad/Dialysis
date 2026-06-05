namespace Dialysis.Module.Contracts.Billing;

/// <summary>
/// The demo dialysis tariff — the single source of truth for what a treatment costs, shared
/// across modules so the chairside <em>live cost estimate</em> (PDMS) and the authoritative
/// <em>invoice</em> (EHR) compute identical numbers and converge.
///
/// Pricing is intentionally simple and itemised: a flat per-session setup fee, a per-minute
/// treatment rate, and a per-litre ultrafiltration consumables rate. Real deployments would
/// replace this with payer-specific contracted rates; this lives next to
/// <see cref="Demo.DemoDataCatalog"/> as cross-module demo/reference data.
/// </summary>
public static class DialysisTariff
{
    /// <summary>The baked-in demo rates used when no override is supplied.</summary>
    public static readonly DialysisTariffOptions Default = new();

    /// <summary>
    /// Computes the itemised cost of a dialysis treatment from its quantities. Quantities are
    /// clamped to non-negative; money lines are rounded to 2 dp (away-from-zero) to match the
    /// EHR <c>Money</c> value object.
    /// </summary>
    /// <param name="modality">Treatment modality (e.g. "HD", "PD") — used only for labelling.</param>
    /// <param name="durationMinutes">Treatment minutes (elapsed for the live estimate, actual for the invoice).</param>
    /// <param name="ufLiters">Ultrafiltration volume removed, in litres.</param>
    /// <param name="options">Optional rate override; defaults to <see cref="Default"/>.</param>
    public static DialysisCostBreakdown Compute(
        string modality,
        int durationMinutes,
        decimal ufLiters,
        DialysisTariffOptions? options = null)
    {
        var rates = options ?? Default;
        var label = string.IsNullOrWhiteSpace(modality) ? "Dialysis" : modality.Trim();
        var minutes = Math.Max(0, durationMinutes);
        var uf = Math.Max(0m, ufLiters);

        var setup = Round(rates.SetupFee);
        var treatment = Round(rates.PerMinuteRate * minutes);
        var consumables = Round(rates.PerLiterUfRate * uf);

        var lines = new List<InvoiceLine>
        {
            new("Treatment setup & vascular access", 1m, "session", setup, setup),
            new($"Dialysis treatment time ({label})", minutes, "min", rates.PerMinuteRate, treatment),
            new("Ultrafiltration consumables", uf, "L", rates.PerLiterUfRate, consumables),
        };

        return new DialysisCostBreakdown(lines, setup + treatment + consumables, rates.CurrencyCode);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Configurable rates behind <see cref="DialysisTariff"/>.</summary>
public sealed class DialysisTariffOptions
{
    /// <summary>Flat fee charged once per session for setup and vascular access.</summary>
    public decimal SetupFee { get; set; } = 120.00m;

    /// <summary>Treatment rate charged per minute of dialysis.</summary>
    public decimal PerMinuteRate { get; set; } = 1.50m;

    /// <summary>Consumables rate charged per litre of ultrafiltration removed.</summary>
    public decimal PerLiterUfRate { get; set; } = 15.00m;

    /// <summary>ISO-4217 currency code for all amounts.</summary>
    public string CurrencyCode { get; set; } = "USD";
}

/// <summary>The itemised result of <see cref="DialysisTariff.Compute"/>.</summary>
public sealed record DialysisCostBreakdown
{
    /// <summary>The itemised result of <see cref="DialysisTariff.Compute"/>.</summary>
    public DialysisCostBreakdown(IReadOnlyList<InvoiceLine> Lines, decimal Total, string CurrencyCode)
    {
        this.Lines = Lines;
        this.Total = Total;
        this.CurrencyCode = CurrencyCode;
    }

    /// <summary>The itemised charge lines.</summary>
    public IReadOnlyList<InvoiceLine> Lines { get; init; }

    /// <summary>Sum of all line amounts.</summary>
    public decimal Total { get; init; }

    /// <summary>ISO-4217 currency code.</summary>
    public string CurrencyCode { get; init; }

    /// <summary>Deconstructs the breakdown.</summary>
    public void Deconstruct(out IReadOnlyList<InvoiceLine> Lines, out decimal Total, out string CurrencyCode)
    {
        Lines = this.Lines;
        Total = this.Total;
        CurrencyCode = this.CurrencyCode;
    }
}

/// <summary>One itemised charge line: quantity × unit price = amount.</summary>
public sealed record InvoiceLine
{
    /// <summary>One itemised charge line: quantity × unit price = amount.</summary>
    public InvoiceLine(string Label, decimal Quantity, string Unit, decimal UnitPrice, decimal Amount)
    {
        this.Label = Label;
        this.Quantity = Quantity;
        this.Unit = Unit;
        this.UnitPrice = UnitPrice;
        this.Amount = Amount;
    }

    /// <summary>Human-readable description of the charge.</summary>
    public string Label { get; init; }

    /// <summary>Quantity billed (minutes, litres, sessions).</summary>
    public decimal Quantity { get; init; }

    /// <summary>Unit the quantity is measured in.</summary>
    public string Unit { get; init; }

    /// <summary>Price per unit.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Extended amount for the line.</summary>
    public decimal Amount { get; init; }

    /// <summary>Deconstructs the line.</summary>
    public void Deconstruct(out string Label, out decimal Quantity, out string Unit, out decimal UnitPrice, out decimal Amount)
    {
        Label = this.Label;
        Quantity = this.Quantity;
        Unit = this.Unit;
        UnitPrice = this.UnitPrice;
        Amount = this.Amount;
    }
}

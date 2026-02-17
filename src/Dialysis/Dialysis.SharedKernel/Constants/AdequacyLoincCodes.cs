namespace Dialysis.SharedKernel.Constants;

/// <summary>
/// LOINC codes for dialysis adequacy and lab monitoring. Used for quality reporting (URR, Kt/V, Hb).
/// </summary>
public static class AdequacyLoincCodes
{
    /// <summary>Urea reduction ratio - dialysis adequacy (target ≥65%).</summary>
    public const string Urr = "30969-2";

    /// <summary>Single pool Kt/V - dialysis adequacy (target ≥1.2).</summary>
    public const string KtV = "49382-4";

    /// <summary>Hemoglobin - anemia management (target 10–12 g/dL).</summary>
    public const string Hemoglobin = "718-7";

    /// <summary>Ferritin - iron stores.</summary>
    public const string Ferritin = "2276-4";

    /// <summary>Transferrin saturation - iron status.</summary>
    public const string Tsat = "2780-1";

    /// <summary>Parathyroid hormone intact.</summary>
    public const string Pth = "2837-9";

    /// <summary>Serum albumin.</summary>
    public const string Albumin = "1751-7";

    /// <summary>Serum potassium.</summary>
    public const string Potassium = "2823-3";

    public static readonly IReadOnlyList<string> AdequacyCodes = [Urr, KtV, Hemoglobin, Ferritin, Tsat, Pth, Albumin, Potassium];
}

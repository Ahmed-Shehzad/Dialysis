using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.BuildingBlocks.DataProtection.Retention;

namespace Dialysis.BuildingBlocks.DataProtection.Ropa;

/// <summary>
/// Operator/DPO-facing projection of the <see cref="RopaDocument"/>. The domain document
/// carries enums (<see cref="LawfulBasis"/>, the <c>[Flags]</c> <see cref="DataCategory"/>) and
/// raw <see cref="RetentionWindow"/> spans; the dashboard and the DPO's PDF binder both want
/// plain humanised strings. This view does that translation once, on the server, so every
/// consumer sees the same legible Art. 30 record and the SPA never has to decode bit-flags or
/// resolve retention keys client-side.
/// </summary>
public sealed record RopaView(
    string ControllerName,
    string ControllerContact,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<RopaModuleSectionView> Modules,
    IReadOnlyList<RetentionWindowView> Retention)
{
    /// <summary>Projects a domain <see cref="RopaDocument"/> into its humanised view.</summary>
    public static RopaView From(RopaDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Index the retention registrations so each activity can resolve its key → a label
        // without re-walking the list per activity.
        var retentionByKey = new Dictionary<string, RetentionWindowRegistration>(StringComparer.OrdinalIgnoreCase);
        foreach (var registration in document.Retention)
        {
            retentionByKey[registration.Key] = registration;
        }

        var modules = document.Modules
            .Select(section => new RopaModuleSectionView(
                section.ModuleSlug,
                section.Activities.Select(activity => ToActivityView(activity, retentionByKey)).ToArray()))
            .ToArray();

        var retention = document.Retention
            .Select(registration => new RetentionWindowView(
                registration.Description,
                FormatWindow(registration.Window),
                registration.Window.LegalAuthority))
            .ToArray();

        return new RopaView(
            document.ControllerName,
            document.ControllerContact,
            document.GeneratedAtUtc,
            modules,
            retention);
    }

    private static ProcessingActivityView ToActivityView(
        ProcessingActivity activity,
        IReadOnlyDictionary<string, RetentionWindowRegistration> retentionByKey)
    {
        string? retentionWindow = null;
        if (!string.IsNullOrWhiteSpace(activity.RetentionKey)
            && retentionByKey.TryGetValue(activity.RetentionKey, out var registration))
        {
            retentionWindow = FormatWindow(registration.Window);
        }

        return new ProcessingActivityView(
            activity.ActivityName,
            activity.Purpose,
            Humanize(activity.Basis),
            ExpandCategories(activity.Categories),
            activity.RecipientCategories,
            retentionWindow);
    }

    /// <summary>Expands the <c>[Flags]</c> <see cref="DataCategory"/> into legible labels.</summary>
    internal static IReadOnlyList<string> ExpandCategories(DataCategory categories) =>
        Enum.GetValues<DataCategory>()
            .Where(value => value != DataCategory.None && categories.HasFlag(value))
            .Select(Humanize)
            .ToArray();

    internal static string Humanize(LawfulBasis basis) => basis switch
    {
        LawfulBasis.Consent => "Consent — Art. 6(1)(a) / 9(2)(a)",
        LawfulBasis.Contract => "Contract — Art. 6(1)(b)",
        LawfulBasis.LegalObligation => "Legal obligation — Art. 6(1)(c)",
        LawfulBasis.VitalInterests => "Vital interests — Art. 6(1)(d)",
        LawfulBasis.HealthcareProvision => "Healthcare provision — Art. 6(1)(e) / 9(2)(h)",
        LawfulBasis.LegitimateInterests => "Legitimate interests — Art. 6(1)(f)",
        _ => basis.ToString(),
    };

    internal static string Humanize(DataCategory category) => category switch
    {
        DataCategory.Identifying => "Identifying",
        DataCategory.ClinicalHealth => "Clinical health",
        DataCategory.Medication => "Medication",
        DataCategory.DeviceTelemetry => "Device telemetry",
        DataCategory.Financial => "Financial",
        DataCategory.Genetic => "Genetic",
        DataCategory.Operational => "Operational",
        _ => category.ToString(),
    };

    /// <summary>Renders a retention window as "10 years" (min == max) or "10–30 years".</summary>
    internal static string FormatWindow(RetentionWindow window)
    {
        var minYears = ToYears(window.Minimum);
        var maxYears = ToYears(window.Maximum);
        return minYears == maxYears
            ? $"{minYears} years"
            : $"{minYears}–{maxYears} years";
    }

    private static int ToYears(TimeSpan span) =>
        (int)Math.Round(span.TotalDays / 365.25, MidpointRounding.AwayFromZero);
}

/// <summary>One module's section of the RoPA view.</summary>
public sealed record RopaModuleSectionView(
    string ModuleSlug,
    IReadOnlyList<ProcessingActivityView> Activities);

/// <summary>One processing activity, humanised for display.</summary>
public sealed record ProcessingActivityView(
    string Name,
    string Purpose,
    string LawfulBasis,
    IReadOnlyList<string> DataCategories,
    IReadOnlyList<string> Recipients,
    string? RetentionWindow);

/// <summary>One retention-schedule row, humanised for display.</summary>
public sealed record RetentionWindowView(
    string DataCategory,
    string WindowLabel,
    string LegalBasis);

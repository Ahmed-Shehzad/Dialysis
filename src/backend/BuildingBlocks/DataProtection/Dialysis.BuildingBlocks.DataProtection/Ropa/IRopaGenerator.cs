using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.BuildingBlocks.DataProtection.Retention;

namespace Dialysis.BuildingBlocks.DataProtection.Ropa;

/// <summary>
/// Renders the platform's GDPR Art. 30 Records of Processing Activities document. Pulls every
/// registered <see cref="ProcessingActivity"/> from every module's <see cref="ILawfulBasisRegistry"/>
/// plus the platform <see cref="IRetentionSchedule"/> + the registered data-subject-rights
/// endpoints.
///
/// Output is a structured RoPA document; the AspNetCore endpoint serialises to JSON for the
/// operator dashboard and to a downloadable PDF for the DPO's binder.
/// </summary>
public interface IRopaGenerator
{
    RopaDocument Generate();
}

public sealed record RopaDocument
{
    public RopaDocument(string ControllerName,
        string ControllerContact,
        DateTimeOffset GeneratedAtUtc,
        IReadOnlyList<RopaModuleSection> Modules,
        IReadOnlyList<RetentionWindowRegistration> Retention)
    {
        this.ControllerName = ControllerName;
        this.ControllerContact = ControllerContact;
        this.GeneratedAtUtc = GeneratedAtUtc;
        this.Modules = Modules;
        this.Retention = Retention;
    }
    public string ControllerName { get; init; }
    public string ControllerContact { get; init; }
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public IReadOnlyList<RopaModuleSection> Modules { get; init; }
    public IReadOnlyList<RetentionWindowRegistration> Retention { get; init; }
    public void Deconstruct(out string ControllerName, out string ControllerContact, out DateTimeOffset GeneratedAtUtc, out IReadOnlyList<RopaModuleSection> Modules, out IReadOnlyList<RetentionWindowRegistration> Retention)
    {
        ControllerName = this.ControllerName;
        ControllerContact = this.ControllerContact;
        GeneratedAtUtc = this.GeneratedAtUtc;
        Modules = this.Modules;
        Retention = this.Retention;
    }
}

public sealed record RopaModuleSection
{
    public RopaModuleSection(string ModuleSlug,
        IReadOnlyList<ProcessingActivity> Activities)
    {
        this.ModuleSlug = ModuleSlug;
        this.Activities = Activities;
    }
    public string ModuleSlug { get; init; }
    public IReadOnlyList<ProcessingActivity> Activities { get; init; }
    public void Deconstruct(out string ModuleSlug, out IReadOnlyList<ProcessingActivity> Activities)
    {
        ModuleSlug = this.ModuleSlug;
        Activities = this.Activities;
    }
}

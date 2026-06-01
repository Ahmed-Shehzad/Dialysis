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

public sealed record RopaDocument(
    string ControllerName,
    string ControllerContact,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<RopaModuleSection> Modules,
    IReadOnlyList<RetentionWindowRegistration> Retention);

public sealed record RopaModuleSection(
    string ModuleSlug,
    IReadOnlyList<ProcessingActivity> Activities);

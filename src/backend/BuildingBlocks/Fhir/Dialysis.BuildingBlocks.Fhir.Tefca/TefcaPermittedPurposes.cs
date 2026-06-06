namespace Dialysis.BuildingBlocks.Fhir.Tefca;

/// <summary>
/// The TEFCA Common Agreement "Permitted Purposes" — the closed set of reasons a participant may
/// exchange information for. Every cross-organization request (push or pull) declares one; the
/// IAS JWT carries it as the <c>purpose_of_use</c> claim, partner policy bounds which the partner
/// may assert, consent is evaluated against it, and the FHIR <c>AuditEvent.purposeOfEvent</c>
/// records it. Values follow the TEFCA SOP token vocabulary (PascalCase, transport-stable).
/// </summary>
public static class TefcaPermittedPurposes
{
    /// <summary>Care delivery to the individual (the most common reason for exchange).</summary>
    public const string Treatment = "Treatment";

    /// <summary>Billing, claims, and reimbursement activities.</summary>
    public const string Payment = "Payment";

    /// <summary>Quality assessment, care coordination, and other HIPAA "operations".</summary>
    public const string HealthcareOperations = "HealthcareOperations";

    /// <summary>Reporting to a public-health authority (surveillance, registries, mandated reports).</summary>
    public const string PublicHealth = "PublicHealth";

    /// <summary>Determining eligibility for, or administering, government benefit programs.</summary>
    public const string GovernmentBenefitsDetermination = "GovernmentBenefitsDetermination";

    /// <summary>An individual exercising their right of access to their own records (IAS).</summary>
    public const string IndividualAccessServices = "IndividualAccessServices";

    /// <summary>All recognised permitted purposes, in canonical order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Treatment,
        Payment,
        HealthcareOperations,
        PublicHealth,
        GovernmentBenefitsDetermination,
        IndividualAccessServices,
    ];

    /// <summary>True when <paramref name="purpose"/> is one of the recognised permitted purposes (case-insensitive).</summary>
    public static bool IsRecognized(string? purpose) =>
        !string.IsNullOrWhiteSpace(purpose)
        && All.Contains(purpose, StringComparer.OrdinalIgnoreCase);
}

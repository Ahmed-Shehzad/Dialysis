namespace Dialysis.BuildingBlocks.DataProtection.LawfulBases;

/// <summary>
/// GDPR Article 6 (lawful basis for processing) and Article 9 (special-category data including
/// health) require every processing activity to declare why it's lawful. The platform's commands
/// declare a basis via <see cref="LawfulBasisAttribute"/>; the registry below records which bases
/// the command's module has registered as legitimate. Mismatched / missing bases short-circuit
/// command execution with a 403 + audit row (see <c>LawfulBasisGuardBehavior</c>).
/// </summary>
public enum LawfulBasis
{
    /// <summary>
    /// GDPR Art. 6(1)(a) + Art. 9(2)(a) — the data subject has given explicit consent. Always
    /// requires a corresponding consent record in <see cref="Consent.IPatientConsentGateway"/>.
    /// </summary>
    Consent,

    /// <summary>
    /// GDPR Art. 6(1)(b) — processing is necessary for the performance of a contract with the
    /// data subject. Rare for clinical data; used for billing portal interactions.
    /// </summary>
    Contract,

    /// <summary>
    /// GDPR Art. 6(1)(c) — processing is necessary for compliance with a legal obligation.
    /// Examples: billing records retained for HGB §257 (10 y), clinical records for
    /// Berufsordnung §10 (30 y).
    /// </summary>
    LegalObligation,

    /// <summary>
    /// GDPR Art. 6(1)(d) — processing is necessary to protect the vital interests of the data
    /// subject or another person. Used for emergency notifications (e.g. an IV pump alarm
    /// escalated to on-call clinicians).
    /// </summary>
    VitalInterests,

    /// <summary>
    /// GDPR Art. 6(1)(e) + Art. 9(2)(h) — processing is necessary for the performance of a
    /// task carried out in the public interest or in the exercise of official authority, OR for
    /// the provision of health / social care. Headline basis for clinical operations: MAR
    /// writes, vitals capture, discharge letters.
    /// </summary>
    HealthcareProvision,

    /// <summary>
    /// GDPR Art. 6(1)(f) — processing is necessary for the legitimate interests pursued by the
    /// controller or by a third party. Used sparingly for non-clinical operational data
    /// (e.g. system telemetry tied to a clinician account).
    /// </summary>
    LegitimateInterests,
}

/// <summary>
/// Categories of data processed by an activity. Each is mapped to the relevant GDPR / BDSG /
/// PDSG articles by the RoPA generator. Bit-flags so a single activity can declare multiple
/// (e.g. a discharge letter is both clinical health data and identifiable demographic data).
/// </summary>
[Flags]
public enum DataCategory
{
    None = 0,

    /// <summary>Patient demographic identifiers (name, MRN, date of birth, address).</summary>
    Identifying = 1 << 0,

    /// <summary>Clinical health data — GDPR Art. 9 special category.</summary>
    ClinicalHealth = 1 << 1,

    /// <summary>Medication administration / prescription data — GDPR Art. 9 special category.</summary>
    Medication = 1 << 2,

    /// <summary>Device telemetry tied to a patient (vitals, IV pump infusions).</summary>
    DeviceTelemetry = 1 << 3,

    /// <summary>Billing + financial data. GDPR Art. 9 special-category when combined with clinical.</summary>
    Financial = 1 << 4,

    /// <summary>Genetic data — GDPR Art. 9 special category. None of today's slices use this.</summary>
    Genetic = 1 << 5,

    /// <summary>Operational system data (audit logs, telemetry) tied to a clinician.</summary>
    Operational = 1 << 6,
}

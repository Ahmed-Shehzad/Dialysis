namespace Dialysis.Simulation.Drivers;

/// <summary>
/// Per-call lineage carried into every driver method so the HTTP implementation can thread it onto
/// the outbound request (Authorization is added by the message handler; correlation/trace as headers).
/// </summary>
public sealed record DriverContext(string TenantId, string CorrelationId, string TraceId);

// ---- EHR ----------------------------------------------------------------------------------------

/// <summary>Register a patient in the EHR.</summary>
public sealed record RegisterPatientCommand(
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    DateOnly DateOfBirth,
    string? SexAtBirthCode);

/// <summary>The registered patient's real EHR id.</summary>
public sealed record RegisteredPatient(Guid PatientId);

/// <summary>Start an encounter for a patient.</summary>
public sealed record StartEncounterCommand(Guid PatientId, Guid ProviderId, string EncounterClassCode, Guid? AppointmentId);

/// <summary>The started encounter's id.</summary>
public sealed record StartedEncounter(Guid EncounterId);

/// <summary>
/// Close the encounter with its diagnoses + procedures. In the real modules this publishes
/// <c>EncounterClosedIntegrationEvent</c>, which the EHR billing consumer turns into captured charges.
/// </summary>
public sealed record CloseEncounterCommand(
    Guid EncounterId,
    Guid PatientId,
    Guid ProviderId,
    IReadOnlyList<string> DiagnosisIcd10Codes,
    IReadOnlyList<string> ProcedureCptCodes);

/// <summary>One captured charge resulting from closing the encounter.</summary>
public sealed record CapturedCharge(string Code, decimal Amount, string Description);

/// <summary>The closed encounter and the charges billing captured from it.</summary>
public sealed record ClosedEncounter(Guid EncounterId, IReadOnlyList<CapturedCharge> Charges);

/// <summary>Request a referral for a patient to a partner organization.</summary>
public sealed record RequestReferralCommand(Guid PatientId, string DestinationPartnerId, Guid ReferringProviderId, string? ReferralReason);

/// <summary>The requested referral's id.</summary>
public sealed record RequestedReferral(Guid ReferralId);

// ---- HIS ----------------------------------------------------------------------------------------

/// <summary>Book an appointment in the HIS scheduling slice.</summary>
public sealed record BookAppointmentCommand(Guid PatientId, Guid ProviderId, DateTime SlotStartUtc, DateTime SlotEndUtc);

/// <summary>The booked appointment's id.</summary>
public sealed record BookedAppointment(Guid AppointmentId);

/// <summary>Admit a patient to a ward.</summary>
public sealed record AdmitPatientCommand(Guid PatientId, string WardCode);

/// <summary>The admission's id.</summary>
public sealed record AdmittedPatient(Guid AdmissionId);

/// <summary>Discharge an admitted patient.</summary>
public sealed record DischargePatientCommand(Guid AdmissionId);

/// <summary>The discharged admission's id.</summary>
public sealed record DischargedPatient(Guid AdmissionId);

// ---- Lab ----------------------------------------------------------------------------------------

/// <summary>One requested test on a lab order (LOINC-coded).</summary>
public sealed record LabTestRequest(string LoincCode, string Display);

/// <summary>Place a lab order for a patient.</summary>
public sealed record PlaceLabOrderCommand(Guid PatientId, IReadOnlyList<LabTestRequest> Tests, string? Specimen);

/// <summary>The placed order's id and the placer order number used to match a result back.</summary>
public sealed record PlacedLabOrder(Guid OrderId, string PlacerOrderNumber);

/// <summary>One resulted observation.</summary>
public sealed record LabObservation(string LoincCode, string Display, string Value, string? Unit, string? ReferenceRange);

/// <summary>
/// Publish a lab result for a placed order. In the real modules this publishes
/// <c>LabResultReceivedIntegrationEvent</c> on the placer order number.
/// </summary>
public sealed record PublishLabResultCommand(string PlacerOrderNumber, Guid PatientId, IReadOnlyList<LabObservation> Observations);

/// <summary>The published result's placer order number.</summary>
public sealed record PublishedLabResult(string PlacerOrderNumber);

// ---- HIE Documents ------------------------------------------------------------------------------

/// <summary>Upload a generated document into the HIE document store.</summary>
public sealed record UploadDocumentCommand(
    Guid PatientId,
    string Kind,
    string Title,
    string MimeType,
    byte[] Content);

/// <summary>The uploaded document's id.</summary>
public sealed record UploadedDocument(Guid DocumentId);

namespace Dialysis.DataSimulator;

/// <summary>EHR clinical write surface.</summary>
public interface IEhrClient
{
    /// <summary>Registers a patient; returns the EHR patient id.</summary>
    Task<Guid> RegisterPatientAsync(GeneratedPatient patient, CancellationToken cancellationToken);

    /// <summary>Starts an encounter; returns the encounter id.</summary>
    Task<Guid> StartEncounterAsync(Guid patientId, Guid providerId, string encounterClassCode, Guid? appointmentId, CancellationToken cancellationToken);
}

/// <summary>HIS scheduling + patient-flow write surface.</summary>
public interface IHisClient
{
    /// <summary>Books an appointment; returns the appointment id.</summary>
    Task<Guid> BookAppointmentAsync(Guid patientId, Guid providerId, DateTime slotStartUtc, DateTime slotEndUtc, CancellationToken cancellationToken);

    /// <summary>Admits a patient to a ward; returns the admission id.</summary>
    Task<Guid> AdmitPatientAsync(Guid patientId, string wardCode, CancellationToken cancellationToken);
}

/// <summary>Lab order write surface.</summary>
public interface ILabClient
{
    /// <summary>Places a lab order; returns the order id.</summary>
    Task<Guid> PlaceLabOrderAsync(Guid patientId, string? specimen, CancellationToken cancellationToken);
}

/// <summary>HIE document write surface.</summary>
public interface IHieClient
{
    /// <summary>Uploads a document; returns the document id.</summary>
    Task<Guid> UploadDocumentAsync(Guid patientId, string kind, string title, string mimeType, byte[] content, CancellationToken cancellationToken);
}

/// <summary>Typed EHR client.</summary>
public sealed class EhrClient : IEhrClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public EhrClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> RegisterPatientAsync(GeneratedPatient patient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patient);
        return HttpJson.PostReadIdAsync(_client, "api/v1.0/clinical/patients",
            new
            {
                patient.MedicalRecordNumber,
                patient.FamilyName,
                patient.GivenName,
                patient.DateOfBirth,
                patient.SexAtBirthCode,
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Guid> StartEncounterAsync(Guid patientId, Guid providerId, string encounterClassCode, Guid? appointmentId, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/clinical/encounters",
            new { patientId, providerId, encounterClassCode, appointmentId }, cancellationToken);
}

/// <summary>Typed HIS client.</summary>
public sealed class HisClient : IHisClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public HisClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> BookAppointmentAsync(Guid patientId, Guid providerId, DateTime slotStartUtc, DateTime slotEndUtc, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/scheduling/appointments",
            new { patientId, providerId, slotStartUtc, slotEndUtc }, cancellationToken);

    /// <inheritdoc />
    public Task<Guid> AdmitPatientAsync(Guid patientId, string wardCode, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/patient-flow/admissions",
            new { patientId, wardCode }, cancellationToken);
}

/// <summary>Typed Lab client.</summary>
public sealed class LabClient : ILabClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public LabClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> PlaceLabOrderAsync(Guid patientId, string? specimen, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/lab/orders",
            new
            {
                patientId,
                tests = new[]
                {
                    new { loincCode = "718-7", display = "Hemoglobin" },
                    new { loincCode = "2160-0", display = "Creatinine" },
                },
                priority = "Routine",
                specimen,
            },
            cancellationToken);
}

/// <summary>Typed HIE client.</summary>
public sealed class HieClient : IHieClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public HieClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> UploadDocumentAsync(Guid patientId, string kind, string title, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        return HttpJson.PostReadIdAsync(_client, "api/v1.0/documents",
            new
            {
                patientId,
                kind,
                title,
                mimeType,
                base64Content = Convert.ToBase64String(content),
            },
            cancellationToken, "documentId", "id");
    }
}

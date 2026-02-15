using System.Net.Http;
using Dialysis.ApiClients;
using Dialysis.DeviceIngestion.Features.IngestVitals;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace Dialysis.DeviceIngestion.Services;

public interface IFhirObservationWriter
{
    Task<IReadOnlyList<string>> WriteObservationsAsync(string? tenantId, string patientId, string encounterId, string? deviceId, IReadOnlyList<VitalReading> readings, CancellationToken cancellationToken = default);
}

public sealed class FhirObservationWriterOptions
{
    public const string SectionName = "Fhir";
    public string? BaseUrl { get; set; }
}

public sealed class FhirObservationWriter : IFhirObservationWriter
{
    private readonly IFhirApi _fhirApi;
    private readonly FhirObservationWriterOptions _options;

    public FhirObservationWriter(IFhirApi fhirApi, IOptions<FhirObservationWriterOptions> options)
    {
        _fhirApi = fhirApi;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<string>> WriteObservationsAsync(string? tenantId, string patientId, string encounterId, string? deviceId, IReadOnlyList<VitalReading> readings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || readings.Count == 0)
            return Array.Empty<string>();

        var ids = new List<string>();
        foreach (var reading in readings)
        {
            var obs = MapToObservation(patientId, encounterId, deviceId, reading);
            var response = await _fhirApi.CreateObservation(obs, cancellationToken);
            response.EnsureSuccessStatusCode();
            var id = ExtractIdFromLocation(response.Headers.Location?.ToString());
            if (!string.IsNullOrEmpty(id)) ids.Add(id);
        }
        return ids;
    }

    private static Observation MapToObservation(string patientId, string encounterId, string? deviceId, VitalReading reading)
    {
        var obs = new Observation
        {
            Status = ObservationStatus.Final,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding("http://loinc.org", reading.Code, reading.Code)
                ]
            },
            Subject = new ResourceReference($"Patient/{patientId}"),
            Encounter = new ResourceReference($"Encounter/{encounterId}"),
            Effective = new FhirDateTime(reading.Effective.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            Value = new Quantity
            {
                Value = (decimal)(double.TryParse(reading.Value, out var v) ? v : 0),
                Unit = reading.Unit,
                System = "http://unitsofmeasure.org",
                Code = reading.Unit
            }
        };

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            obs.Device = new ResourceReference($"Device/{deviceId}");
        }

        return obs;
    }

    private static string? ExtractIdFromLocation(string? location)
    {
        if (string.IsNullOrEmpty(location)) return null;
        var segments = location.TrimEnd('/').Split('/');
        return segments.Length >= 2 ? segments[^1] : null;
    }
}

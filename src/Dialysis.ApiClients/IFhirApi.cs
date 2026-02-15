using System.Net.Http;
using Hl7.Fhir.Model;
using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for FHIR Gateway (Azure Health Data Services or compatible).</summary>
public interface IFhirApi
{
    [Get("/Encounter")]
    Task<Bundle> SearchEncounters(
        [Query(CollectionFormat.Multi)] string[]? date = null,
        [Query] string? _summary = null,
        [Query] int? _count = null,
        [Query] string? _elements = null,
        CancellationToken cancellationToken = default);

    [Get("/Observation")]
    Task<Bundle> SearchObservations(
        [Query] string? code = null,
        [Query(CollectionFormat.Multi)] string[]? date = null,
        [Query] string? encounter = null,
        [Query] int? _count = null,
        [Query] string? _elements = null,
        CancellationToken cancellationToken = default);

    [Get("/Patient")]
    Task<Bundle> SearchPatients(
        [Query] int? _count = null,
        [Query] string? _elements = null,
        CancellationToken cancellationToken = default);

    [Get("/Patient/{id}")]
    Task<Patient> GetPatient(string id, CancellationToken cancellationToken = default);

    [Get("/Encounter/{id}")]
    Task<Encounter> GetEncounter(string id, CancellationToken cancellationToken = default);

    [Get("/Observation/{id}")]
    Task<Observation> GetObservation(string id, CancellationToken cancellationToken = default);

    [Get("/Composition/{id}")]
    Task<Composition> GetComposition(string id, CancellationToken cancellationToken = default);

    [Get("/MeasureReport/{id}")]
    Task<MeasureReport> GetMeasureReport(string id, CancellationToken cancellationToken = default);

    [Get("/Bundle/{id}")]
    Task<Bundle> GetBundle(string id, CancellationToken cancellationToken = default);

    [Get("/DocumentReference/{id}")]
    Task<DocumentReference> GetDocumentReference(string id, CancellationToken cancellationToken = default);

    [Post("/Observation")]
    Task<HttpResponseMessage> CreateObservation([Body] Observation observation, CancellationToken cancellationToken = default);

    [Post("/Patient")]
    Task<HttpResponseMessage> CreatePatient([Body] Patient patient, CancellationToken cancellationToken = default);

    [Post("/Encounter")]
    Task<HttpResponseMessage> CreateEncounter([Body] Encounter encounter, CancellationToken cancellationToken = default);

    [Post("/Provenance")]
    Task<HttpResponseMessage> CreateProvenance([Body] Provenance provenance, CancellationToken cancellationToken = default);

    [Post("/DocumentReference")]
    Task<HttpResponseMessage> CreateDocumentReference([Body] DocumentReference documentReference, CancellationToken cancellationToken = default);

    [Post("/$convert-data")]
    Task<Bundle> ConvertData([Body] Parameters parameters, CancellationToken cancellationToken = default);

    /// <summary>POST Bundle transaction to FHIR base.</summary>
    [Post("")]
    Task<HttpResponseMessage> PostTransaction([Body] Bundle bundle, CancellationToken cancellationToken = default);
}

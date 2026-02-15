using Dialysis.ApiClients;
using Dialysis.Documents.Configuration;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Documents.Services;

/// <summary>Stores and retrieves PDF documents in FHIR as Binary + DocumentReference.</summary>
public sealed class FhirDocumentStore : IDocumentStore
{
    private readonly IFhirApi _fhirApi;
    private readonly IFhirBinaryClient _binaryClient;
    private readonly DocumentsOptions _options;
    private readonly ILogger<FhirDocumentStore> _logger;

    public FhirDocumentStore(
        IFhirApi fhirApi,
        IFhirBinaryClient binaryClient,
        IOptions<DocumentsOptions> options,
        ILogger<FhirDocumentStore> logger)
    {
        _fhirApi = fhirApi;
        _binaryClient = binaryClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DocumentStoreResult> StoreAsync(
        byte[] pdfContent,
        string patientId,
        string documentTypeLoinc,
        string? encounterId,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _options.FhirBaseUrl.TrimEnd('/');
        var binaryId = await CreateBinaryAsync(pdfContent, cancellationToken);
        var docRef = BuildDocumentReference(patientId, documentTypeLoinc, encounterId, description, baseUrl, binaryId);
        var response = await _fhirApi.CreateDocumentReference(docRef, cancellationToken);
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location?.ToString() ?? "";
        var docRefId = location.Split('/').LastOrDefault() ?? binaryId;
        return new DocumentStoreResult(docRefId, binaryId);
    }

    public async Task<DocumentReferenceInfo?> GetAsync(string documentReferenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var docRef = await _fhirApi.GetDocumentReference(documentReferenceId, cancellationToken);
            var patientId = docRef.Subject?.Reference?.Split('/').LastOrDefault() ?? "";
            var docType = docRef.Type?.Coding?.FirstOrDefault()?.Code ?? "";
            var encounterId = docRef.Context?.Encounter?.FirstOrDefault()?.Reference?.Split('/').LastOrDefault();
            var date = docRef.DateElement?.Value?.UtcDateTime;
            var desc = docRef.Description;
            return new DocumentReferenceInfo(docRef.Id ?? documentReferenceId, patientId, docType, encounterId, date, desc);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "DocumentReference {Id} not found", documentReferenceId);
            return null;
        }
    }

    public async Task<byte[]?> GetContentAsync(string documentReferenceId, CancellationToken cancellationToken = default)
    {
        var docRef = await _fhirApi.GetDocumentReference(documentReferenceId, cancellationToken);
        var attachment = docRef.Content?.FirstOrDefault()?.Attachment;
        if (attachment == null) return null;
        if (attachment.Data != null) return attachment.Data;
        if (!string.IsNullOrEmpty(attachment.Url))
        {
            var url = attachment.Url;
            if (url.StartsWith("/")) url = _options.FhirBaseUrl.TrimEnd('/') + url;
            return await _binaryClient.GetAsync(url, cancellationToken);
        }
        return null;
    }

    private async Task<string> CreateBinaryAsync(byte[] content, CancellationToken cancellationToken)
    {
        var id = await _binaryClient.CreateBinaryAsync(content, "application/pdf", cancellationToken);
        if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("Binary created but no ID returned");
        return id;
    }

    private static DocumentReference BuildDocumentReference(
        string patientId,
        string documentTypeLoinc,
        string? encounterId,
        string? description,
        string baseUrl,
        string binaryId)
    {
        var docRef = new DocumentReference
        {
            Status = DocumentReferenceStatus.Current,
            DocStatus = CompositionStatus.Final,
            Type = new CodeableConcept
            {
                Coding = [new Coding("http://loinc.org", documentTypeLoinc, documentTypeLoinc)]
            },
            Subject = new ResourceReference($"Patient/{patientId}"),
            DateElement = new Instant(DateTimeOffset.UtcNow),
            Description = description,
            Content =
            [
                new DocumentReference.ContentComponent
                {
                    Attachment = new Attachment
                    {
                        ContentType = "application/pdf",
                        Url = $"{baseUrl}/Binary/{binaryId}"
                    }
                }
            ]
        };
        if (!string.IsNullOrEmpty(encounterId))
            docRef.Context = new DocumentReference.ContextComponent { Encounter = [new ResourceReference($"Encounter/{encounterId}")] };
        return docRef;
    }
}

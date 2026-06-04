using System.Net.Http.Headers;
using System.Text.Json;
using Dialysis.BuildingBlocks.DataProtection.Consent;
using Dialysis.BuildingBlocks.Tefca.Ti.Endpoints;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Tefca.Ti.Epa;

/// <summary>
/// Production gematik ePA upload. Posts a document into the patient's
/// <em>elektronische Patientenakte</em> via the gematik ePA REST gateway selected by
/// <see cref="ITelematikInfrastrukturClient.Environment"/>. Gated by the PDSG
/// consent check (<see cref="IPatientConsentGateway"/>, scope
/// <see cref="ConsentScope.EpaDocument"/>) — refused without explicit per-document
/// consent.
///
/// Auth model: the SMC-B-backed handshake (PR 1 scaffold) yields an access token via the
/// gematik IDP. This service requests a fresh token via the TI client on each upload — ePA
/// tokens are short-lived (typically 10 min) and not worth caching at the service layer
/// when the underlying client handles refresh.
///
/// Wire shape: multipart/form-data with the document binary plus a JSON metadata part
/// declaring <c>patientId</c>, <c>title</c>, <c>mimeType</c>, and <c>purpose</c>. The
/// gematik ePA gateway returns 201 with <c>{ "documentId": "..." }</c> on success.
/// </summary>
public sealed class GematikEpaUploadService : IEpaUploadService
{
    private readonly ITelematikInfrastrukturClient _ti;
    private readonly IPatientConsentGateway _consent;
    private readonly IGematikAccessTokenSource _tokens;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GematikEpaUploadService> _logger;
    /// <summary>
    /// Production gematik ePA upload. Posts a document into the patient's
    /// <em>elektronische Patientenakte</em> via the gematik ePA REST gateway selected by
    /// <see cref="ITelematikInfrastrukturClient.Environment"/>. Gated by the PDSG
    /// consent check (<see cref="IPatientConsentGateway"/>, scope
    /// <see cref="ConsentScope.EpaDocument"/>) — refused without explicit per-document
    /// consent.
    ///
    /// Auth model: the SMC-B-backed handshake (PR 1 scaffold) yields an access token via the
    /// gematik IDP. This service requests a fresh token via the TI client on each upload — ePA
    /// tokens are short-lived (typically 10 min) and not worth caching at the service layer
    /// when the underlying client handles refresh.
    ///
    /// Wire shape: multipart/form-data with the document binary plus a JSON metadata part
    /// declaring <c>patientId</c>, <c>title</c>, <c>mimeType</c>, and <c>purpose</c>. The
    /// gematik ePA gateway returns 201 with <c>{ "documentId": "..." }</c> on success.
    /// </summary>
    public GematikEpaUploadService(ITelematikInfrastrukturClient ti,
        IPatientConsentGateway consent,
        IGematikAccessTokenSource tokens,
        IHttpClientFactory httpClientFactory,
        ILogger<GematikEpaUploadService> logger)
    {
        _ti = ti;
        _consent = consent;
        _tokens = tokens;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    public async Task<EpaUploadResult> UploadAsync(
        EpaUploadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // PDSG gate first — refused uploads never leave the platform.
        var decision = await _consent.AuthoriseAsync(
            request.PatientId,
            purpose: request.Purpose,
            scope: ConsentScope.EpaDocument,
            targetDocumentId: null,
            cancellationToken).ConfigureAwait(false);
        if (!decision.IsGranted)
        {
            _logger.LogInformation(
                "Refusing ePA upload for patient {PatientId}: {Reason}",
                request.PatientId, decision.Reason);
            return new EpaUploadResult(false, null, $"PDSG consent denied: {decision.Reason}");
        }

        try
        {
            var endpoint = GematikEndpointCatalog.For(_ti.Environment);
            var accessToken = await _tokens.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

            using var client = _httpClientFactory.CreateClient("tefca-ti-epa-upload");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var multipart = new MultipartFormDataContent();
            var metadata = JsonSerializer.Serialize(new
            {
                patientId = request.PatientId,
                title = request.DocumentTitle,
                mimeType = request.MimeType,
                purpose = request.Purpose,
                actorSub = request.ActorSub,
                language = string.IsNullOrWhiteSpace(request.Language) ? "de" : request.Language,
            });
            using var metadataContent = new StringContent(metadata);
            metadataContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            multipart.Add(metadataContent, "metadata");

            using var documentContent = new ByteArrayContent(request.Content.ToArray());
            documentContent.Headers.ContentType = new MediaTypeHeaderValue(request.MimeType);
            multipart.Add(documentContent, "document", request.DocumentTitle);

            using var response = await client.PostAsync(endpoint.EpaUpload, multipart, cancellationToken)
                .ConfigureAwait(false);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ePA upload rejected for patient {PatientId}: HTTP {Status} {Body}",
                    request.PatientId, (int)response.StatusCode, Truncate(raw));
                return new EpaUploadResult(false, null, $"HTTP {(int)response.StatusCode}");
            }

            string? documentId = null;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("documentId", out var idEl))
                    documentId = idEl.GetString();
            }
            catch (JsonException) { /* ePA gateway response shape is informational — success is the 2xx. */ }

            _logger.LogInformation(
                "ePA upload succeeded for patient {PatientId}; documentId {DocumentId}.",
                request.PatientId, documentId ?? "<unparsed>");
            return new EpaUploadResult(true, documentId, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ePA upload threw for patient {PatientId}.", request.PatientId);
            return new EpaUploadResult(false, null, ex.GetType().Name);
        }
    }

    private static string Truncate(string s) => s.Length <= 512 ? s : s[..512];
}

/// <summary>
/// Supplies an access token for the gematik IDP-issued OIDC bearer. The TI client owns
/// the actual handshake (SMC-B challenge + token exchange) — this is the source-of-truth
/// abstraction the upload service consumes so we don't have to plumb the TI client's
/// internals through the upload path.
/// </summary>
public interface IGematikAccessTokenSource
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

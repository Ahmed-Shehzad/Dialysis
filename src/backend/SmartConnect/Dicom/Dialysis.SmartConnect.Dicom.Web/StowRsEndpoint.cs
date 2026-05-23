using FellowOakDicom;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Dialysis.SmartConnect.Dicom.Web;

/// <summary>
/// DICOMweb STOW-RS endpoint (PS3.18 §10.5). Accepts <c>POST /studies</c> with
/// <c>multipart/related; type="application/dicom"</c>; each part is a single .dcm file. Each part
/// is parsed and ingested via <see cref="IDicomIngestionService"/>.
/// </summary>
/// <remarks>
/// The response shape is a 200 OK with a small JSON summary rather than the full XDS-SD
/// multipart response in the standard. Adequate for the internal-network deployment; cross-
/// vendor STOW-RS would need the standards-compliant response builder.
/// </remarks>
public static class StowRsEndpoint
{
    public static IEndpointRouteBuilder MapStowRs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/studies", HandleStowAsync)
            .WithName("StowRsStoreStudies")
            .Accepts<IFormFile>("multipart/related")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status415UnsupportedMediaType);
        return endpoints;
    }

    /// <summary>
    /// Public to satisfy ASP.NET Core's minimal-API surface — the request delegate is reachable
    /// from <see cref="MapStowRs"/>.
    /// </summary>
    public static async Task<IResult> HandleStowAsync(
        HttpRequest request,
        [FromServices] IDicomIngestionService ingestion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(ingestion);

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)
            || !string.Equals(contentType.MediaType.ToString(), "multipart/related", StringComparison.OrdinalIgnoreCase))
        {
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
        if (string.IsNullOrEmpty(boundary))
        {
            return Results.BadRequest(new { error = "missing boundary" });
        }

        var reader = new MultipartReader(boundary, request.Body);
        var stored = new List<DicomInstanceMetadata>();
        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (!IsDicomPart(section))
            {
                continue;
            }
            using var buffer = new MemoryStream();
            await section.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;
            var dcm = await DicomFile.OpenAsync(buffer).ConfigureAwait(false);
            var metadata = await ingestion.IngestAsync(dcm, cancellationToken).ConfigureAwait(false);
            stored.Add(metadata);
        }

        return Results.Ok(new
        {
            stored = stored.Count,
            instances = stored.Select(s => new
            {
                studyInstanceUid = s.StudyInstanceUid,
                seriesInstanceUid = s.SeriesInstanceUid,
                sopInstanceUid = s.SopInstanceUid,
            }),
        });
    }

    private static bool IsDicomPart(MultipartSection section)
    {
        var part = section.ContentType;
        if (string.IsNullOrEmpty(part)) return false;
        return part.StartsWith("application/dicom", StringComparison.OrdinalIgnoreCase);
    }
}

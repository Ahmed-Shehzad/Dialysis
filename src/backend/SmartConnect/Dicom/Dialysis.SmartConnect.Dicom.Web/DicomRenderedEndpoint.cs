using Dialysis.SmartConnect.Attachments;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SixLabors.ImageSharp;

namespace Dialysis.SmartConnect.Dicom.Web;

/// <summary>
/// WADO-RS rendered retrieval (PS3.18 §10.4.1.1.2) — returns a frame rendered to PNG rather than the
/// raw DICOM, so the EHR chart imaging viewer is a plain <c>&lt;img&gt;</c> (no client-side DICOM
/// decoder). Rendering is managed (fo-dicom ImageSharp backend, no native codecs), so compressed
/// transfer syntaxes we can't decode return 415 and the SPA degrades gracefully.
/// <list type="bullet">
///   <item><c>GET /studies/{study}/series/{series}/instances/{sop}/rendered</c></item>
///   <item><c>GET /studies/{study}/rendered</c> — first instance of the study (preview convenience).</item>
/// </list>
/// </summary>
public static class DicomRenderedEndpoint
{
    private static int _imageManagerConfigured;

    /// <summary>Maps the rendered retrieval routes.</summary>
    public static IEndpointRouteBuilder MapDicomRenderedRs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/studies/{studyUid}/series/{seriesUid}/instances/{sopUid}/rendered", HandleInstanceRenderedAsync);
        endpoints.MapGet("/studies/{studyUid}/rendered", HandleStudyRenderedAsync);
        return endpoints;
    }

    /// <summary>Renders a single instance to PNG.</summary>
    public static async Task<IResult> HandleInstanceRenderedAsync(
        string studyUid, string seriesUid, string sopUid,
        [FromServices] IDicomInstanceStore instances,
        [FromServices] IAttachmentBlobStore blobs,
        CancellationToken cancellationToken)
    {
        var metadata = await instances.GetAsync(sopUid, cancellationToken).ConfigureAwait(false);
        if (metadata is null || metadata.StudyInstanceUid != studyUid || metadata.SeriesInstanceUid != seriesUid)
        {
            return Results.NotFound();
        }
        return await RenderAsync(metadata.BlobId, blobs, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Renders the first instance of a study to PNG (preview thumbnail for the chart card).</summary>
    public static async Task<IResult> HandleStudyRenderedAsync(
        string studyUid,
        [FromServices] IDicomInstanceStore instances,
        [FromServices] IAttachmentBlobStore blobs,
        CancellationToken cancellationToken)
    {
        var metadata = await instances.GetByStudyAsync(studyUid, cancellationToken).ConfigureAwait(false);
        if (metadata.Count == 0)
        {
            return Results.NotFound();
        }
        return await RenderAsync(metadata[0].BlobId, blobs, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IResult> RenderAsync(Guid blobId, IAttachmentBlobStore blobs, CancellationToken cancellationToken)
    {
        var bytes = await blobs.ReadAsync(blobId, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return Results.NotFound();
        }

        EnsureImageManager();
        try
        {
            var file = await DicomFile.OpenAsync(new MemoryStream(bytes.Value.ToArray())).ConfigureAwait(false);
            using var image = new DicomImage(file.Dataset).RenderImage(0).AsSharpImage();
            var output = new MemoryStream();
            await image.SaveAsPngAsync(output, cancellationToken).ConfigureAwait(false);
            output.Position = 0;
            return Results.File(output, contentType: "image/png");
        }
        catch (Exception ex) when (ex is DicomImagingException or DicomFileException or NotSupportedException or InvalidOperationException)
        {
            // Compressed transfer syntaxes need native codecs we don't ship — surface as unsupported
            // so the SPA shows "preview unavailable" rather than a 500.
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }
    }

    private static void EnsureImageManager()
    {
        // fo-dicom 5 resolves the IImageManager from its static service provider; configure it once
        // with the managed ImageSharp backend. No other code sets up fo-dicom DI, so this is safe.
        if (Interlocked.Exchange(ref _imageManagerConfigured, 1) == 0)
        {
            new DicomSetupBuilder()
                .RegisterServices(s => s.AddImageManager<ImageSharpImageManager>())
                .Build();
        }
    }
}

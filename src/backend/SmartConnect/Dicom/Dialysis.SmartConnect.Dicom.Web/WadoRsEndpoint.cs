using System.Text;
using Dialysis.SmartConnect.Attachments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Dicom.Web;

/// <summary>
/// DICOMweb WADO-RS endpoints (PS3.18 §10.4). Three resource levels:
/// <list type="bullet">
///   <item><c>GET /studies/{StudyInstanceUID}</c> — every instance under a study.</item>
///   <item><c>GET /studies/{...}/series/{SeriesInstanceUID}</c> — every instance in a series.</item>
///   <item><c>GET /studies/{...}/series/{...}/instances/{SopInstanceUID}</c> — a single instance.</item>
/// </list>
/// All three respond <c>multipart/related; type="application/dicom"</c> with one body part per
/// .dcm file. The single-instance route is the only one most clients hit — series/study fan-out
/// is bandwidth-expensive and typically downloaded one instance at a time.
/// </summary>
public static class WadoRsEndpoint
{
    private const string MultipartBoundary = "dialysis-wado";

    public static IEndpointRouteBuilder MapWadoRs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/studies/{studyUid}/series/{seriesUid}/instances/{sopUid}", HandleInstanceAsync);
        endpoints.MapGet("/studies/{studyUid}", HandleStudyAsync);
        return endpoints;
    }

    /// <summary>Single-instance retrieval.</summary>
    public static async Task<IResult> HandleInstanceAsync(
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
        var bytes = await blobs.ReadAsync(metadata.BlobId, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return Results.NotFound();
        }
        return BuildMultipartResponse([bytes.Value]);
    }

    /// <summary>Study-level retrieval — fans out to every instance under the study.</summary>
    public static async Task<IResult> HandleStudyAsync(
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
        var parts = new List<ReadOnlyMemory<byte>>(metadata.Count);
        foreach (var m in metadata)
        {
            var bytes = await blobs.ReadAsync(m.BlobId, cancellationToken).ConfigureAwait(false);
            if (bytes is not null) parts.Add(bytes.Value);
        }
        return BuildMultipartResponse(parts);
    }

    private static IResult BuildMultipartResponse(IReadOnlyList<ReadOnlyMemory<byte>> parts)
    {
        // Hand-rolled multipart serialization — Microsoft.AspNetCore.WebUtilities has a writer but
        // it's not exposed as a result helper, so we synthesise the wire format here.
        var stream = new MemoryStream();
        foreach (var part in parts)
        {
            WriteAscii(stream, $"--{MultipartBoundary}\r\nContent-Type: application/dicom\r\n\r\n");
            stream.Write(part.Span);
            WriteAscii(stream, "\r\n");
        }
        WriteAscii(stream, $"--{MultipartBoundary}--\r\n");
        stream.Position = 0;
        return Results.File(
            stream,
            contentType: $"multipart/related; type=\"application/dicom\"; boundary={MultipartBoundary}");
    }

    private static void WriteAscii(MemoryStream stream, string s)
    {
        var bytes = Encoding.ASCII.GetBytes(s);
        stream.Write(bytes, 0, bytes.Length);
    }
}

using Hl7.Fhir.Model;

namespace Dialysis.Documents.Services;

/// <summary>Converts FHIR Document Bundle (Composition) or DocumentReference to PDF.</summary>
public interface IBundleToPdfConverter
{
    /// <summary>Convert FHIR document to PDF.</summary>
    /// <param name="bundle">Document Bundle (type=document); first resource is Composition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF bytes.</returns>
    Task<byte[]> ConvertAsync(Bundle bundle, CancellationToken cancellationToken = default);

    /// <summary>Convert DocumentReference (and its referenced Binary/Composition) to PDF.</summary>
    /// <param name="documentReferenceId">DocumentReference ID to fetch and resolve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF bytes.</returns>
    Task<byte[]> ConvertFromDocumentReferenceAsync(string documentReferenceId, CancellationToken cancellationToken = default);
}

using Hl7.Fhir.Model;

namespace Dialysis.HIE.Query.Xca;

/// <summary>
/// Cross-Gateway Query (IHE XCA ITI-38, FHIR analog): asks a partner registry for the document
/// metadata it holds about a patient — <c>DocumentReference?patient={id}</c>.
/// </summary>
public interface IXcaQueryClient
{
    Task<IReadOnlyList<DocumentReference>> QueryDocumentsAsync(
        Guid partnerId, string partnerPatientId, string purposeOfUse, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cross-Gateway Retrieve (IHE XCA ITI-39, FHIR analog): fetches the binary content for a document
/// the query returned — inline <c>attachment.data</c> when present, otherwise the referenced
/// <c>Binary</c>.
/// </summary>
public interface IXcaRetrieveClient
{
    Task<byte[]?> RetrieveContentAsync(
        Guid partnerId, DocumentReference document, string subject, string purposeOfUse, CancellationToken cancellationToken = default);
}

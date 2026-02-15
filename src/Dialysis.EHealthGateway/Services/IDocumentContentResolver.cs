namespace Dialysis.EHealthGateway.Services;

/// <summary>Resolves DocumentReference ID to PDF byte content (via Documents API or FHIR).</summary>
public interface IDocumentContentResolver
{
    /// <summary>Fetch PDF content for the given DocumentReference ID. Returns null if not found or resolver not configured.</summary>
    Task<byte[]?> ResolveAsync(string documentReferenceId, CancellationToken cancellationToken = default);
}

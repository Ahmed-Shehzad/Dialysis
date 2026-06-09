namespace Dialysis.BuildingBlocks.Documents.Signing.Hosted;

/// <summary>
/// Module-supplied job that promotes outstanding PAdES-B-T signatures to LTA. The HIE module
/// implements this by streaming <c>DocumentReferenceSignature</c> rows, re-fetching the PDF bytes
/// through the shared <c>IDocumentBlobStore</c>, running the augmenter, and updating the row via
/// <c>DocumentReferenceSignature.UpgradeLevel</c>. Driven by a persistent Hangfire recurring job
/// (opt-in via <c>Documents:Signing:Ltv:AutoUpgrade</c>).
/// </summary>
public interface ILtvUpgradeJob
{
    /// <summary>Runs one upgrade pass; returns the number of rows promoted.</summary>
    Task<int> RunOnceAsync(CancellationToken cancellationToken);
}

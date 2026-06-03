using Dialysis.BuildingBlocks.Documents.Storage;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Adapter mapping the PDMS-narrow <see cref="IReportBlobStore"/> port onto the cross-module
/// <see cref="IDocumentBlobStore"/>. PDMS continues to own its <c>SessionReport</c> aggregate
/// and call <see cref="IReportBlobStore"/>; HIE Documents reads through the same shared
/// <see cref="IDocumentBlobStore"/>, so both modules resolve the same bytes by the same
/// storage-ref without a redundant per-module store. The class name is unchanged for
/// backwards compatibility with existing composition wiring.
/// </summary>
public sealed class InMemoryReportBlobStore : IReportBlobStore
{
    private readonly IDocumentBlobStore _inner;

    public InMemoryReportBlobStore() : this(new InMemoryDocumentBlobStore()) { }

    public InMemoryReportBlobStore(IDocumentBlobStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public Task<string> SaveAsync(Guid reportId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken) =>
        _inner.SaveAsync(reportId, contentType, body, cancellationToken);

    public Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken) =>
        _inner.ReadAsync(storageRef, cancellationToken);
}

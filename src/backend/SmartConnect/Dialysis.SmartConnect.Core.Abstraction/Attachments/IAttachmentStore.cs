namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// High-level attachment facade. Composes metadata persistence with an <see cref="IAttachmentBlobStore"/>
/// for byte storage; consumers (handlers, reattach service, API) use this without caring where bytes live.
/// </summary>
public interface IAttachmentStore
{
    Task<Attachment> AddAsync(Attachment attachment, CancellationToken cancellationToken);

    Task<Attachment?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Attachment>> GetForMessageAsync(Guid messageId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task DeleteForMessageAsync(Guid messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Bulk delete of rows older than <paramref name="cutoffUtc"/>. Called from <c>DataPrunerHostedService</c>.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken);
}

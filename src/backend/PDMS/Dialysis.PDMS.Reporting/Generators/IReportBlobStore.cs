namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Persists rendered report bytes outside the relational DB — the aggregate carries only the
/// content hash + storage ref. Production hosts back this with S3 / Azure Blob; dev defaults
/// to a local-filesystem store.
/// </summary>
public interface IReportBlobStore
{
    /// <summary>Returns the storage-ref handle for the persisted body.</summary>
    Task<string> SaveAsync(Guid reportId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken);

    Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken);
}

/// <summary>Repository for the <see cref="Domain.SessionReport"/> aggregate.</summary>
public interface ISessionReportRepository
{
    void Add(Domain.SessionReport report);
    Task<Domain.SessionReport?> FindAsync(Guid reportId, CancellationToken cancellationToken);
}

/// <summary>Repository for the operator-authored <see cref="Domain.ReportTemplate"/>.</summary>
public interface IReportTemplateRepository
{
    Task<Domain.ReportTemplate?> FindActiveAsync(Domain.ReportKind kind, CancellationToken cancellationToken);
}

/// <summary>
/// Cross-module read port — builds the flat report context from session + MAR + alarms.
/// Implemented in the composition root because it needs to pull from multiple aggregates.
/// </summary>
public interface ISessionReportContextBuilder
{
    Task<SessionReportContext?> BuildAsync(Guid sessionId, CancellationToken cancellationToken);
}

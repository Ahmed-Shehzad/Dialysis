namespace BuildingBlocks.Abstractions;

/// <summary>
/// Stores audit records for retrieval (e.g. as FHIR AuditEvent Bundle).
/// Used by FhirAuditRecorder to persist events for later FHIR export.
/// </summary>
public interface IAuditEventStore
{
    /// <summary>
    /// Appends an audit record to the store.
    /// </summary>
    Task AppendAsync(AuditRecordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent audit records.
    /// </summary>
    /// <param name="count">Maximum number of records to return.</param>
    Task<IReadOnlyList<AuditRecordRequest>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
}

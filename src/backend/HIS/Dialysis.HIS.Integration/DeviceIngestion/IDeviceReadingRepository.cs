namespace Dialysis.HIS.Integration.DeviceIngestion;

public sealed class DeviceReadingRecord
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public Guid PatientId { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>Optional client-supplied deduplication key (idempotent re-ingest).</summary>
    public string? ExternalMessageId { get; set; }
}

public interface IDeviceReadingRepository
{
    Task<Guid?> FindIdByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists <paramref name="record"/> (together with any other pending changes on the shared
    /// unit of work, e.g. the device's last-seen stamp) and returns its id. If a concurrent ingest
    /// has already inserted a row with the same <see cref="DeviceReadingRecord.ExternalMessageId"/>
    /// — so the unique index rejects this one — the existing row's id is returned instead of
    /// surfacing the constraint violation, keeping idempotent re-ingest correct under races.
    /// </summary>
    Task<Guid> PersistIdempotentAsync(DeviceReadingRecord record, CancellationToken cancellationToken = default);
}

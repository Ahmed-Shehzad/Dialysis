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
    void Add(DeviceReadingRecord record);

    Task<Guid?> FindIdByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default);
}

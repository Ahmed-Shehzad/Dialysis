namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Summary of a blob known to an <see cref="IAttachmentBlobStore"/>. Returned from
/// <see cref="IAttachmentBlobStore.EnumerateAsync"/> so the orphan reaper can decide what to delete
/// without loading the bytes themselves. <see cref="CreatedUtc"/> drives the age-based grace window
/// that prevents reaping a blob whose metadata insert hasn't committed yet.
/// </summary>
public sealed record BlobMetadata(Guid Id, DateTimeOffset CreatedUtc, long SizeBytes);

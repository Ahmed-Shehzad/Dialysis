namespace Dialysis.SmartConnect;

/// <summary>
/// Deployable integration pipeline: inbound acceptance, route filters, outbound routes with transforms.
/// </summary>
public sealed class IntegrationFlow
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public FlowRuntimeState RuntimeState { get; init; }

    public required IntegrationFlowPipelineDefinition Pipeline { get; init; }

    /// <summary>User-defined labels for categorization and filtering.</summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>Optional group assignment for organizational grouping.</summary>
    public Guid? GroupId { get; init; }

    /// <summary>Free-text description of the flow's purpose.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Declared accepted payload formats. Operator-facing metadata used by the UI for filtering
    /// and by the Create-Channel dialog to seed appropriate verify-* plugins. Allowed values:
    /// <c>HL7v2</c>, <c>FHIR</c>, <c>NCPDP</c>, <c>JSON</c>, <c>XML</c>, <c>Binary</c>, <c>Other</c>.
    /// Empty list means "no constraints declared".
    /// </summary>
    public List<string> DataTypes { get; init; } = [];

    /// <summary>
    /// Flow ids this channel depends on. The Start endpoint refuses to transition this flow into
    /// the <see cref="FlowRuntimeState.Started"/> state unless every referenced flow is already
    /// <see cref="FlowRuntimeState.Started"/> (or <c>?force=true</c> is supplied).
    /// </summary>
    public List<Guid> Dependencies { get; init; } = [];

    /// <summary>
    /// Channel-level reference attachments — sample messages, FHIR profile JSON, vendor docs.
    /// Declarative only; not replayed automatically. The per-message attachment store
    /// (<see cref="IntegrationFlowPipelineDefinition.AttachmentHandler"/>) covers the runtime path.
    /// </summary>
    public List<ChannelAttachmentReference> Attachments { get; init; } = [];
}

/// <summary>
/// Channel-level reference attachment. Inline payloads (base64) ride on the flow row with a hard
/// 1 MiB cap; payloads bigger than that go through the blob backend pointed at by
/// <see cref="StorageRef"/> (the same blob store the per-message attachment handler uses).
/// </summary>
public sealed class ChannelAttachmentReference
{
    public required string Name { get; init; }

    public required string MimeType { get; init; }

    /// <summary>
    /// Base64-encoded inline contents. Capped at 1 MiB (decoded). Empty when the attachment is
    /// stored out-of-row — see <see cref="StorageRef"/>.
    /// </summary>
    public required string Base64Bytes { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// Optional pointer to an out-of-row blob. When set, the 1 MiB inline cap is waived because
    /// the bytes don't ride on the flow row — they're streamed from the configured
    /// <c>IAttachmentBlobStore</c> on demand.
    /// </summary>
    public ChannelAttachmentStorageRef? StorageRef { get; init; }
}

/// <summary>
/// Pointer to an out-of-row attachment blob. Today only <c>blob</c> is supported (uses the
/// configured <c>IAttachmentBlobStore</c>). The discriminator leaves room for future kinds
/// (e.g. <c>s3</c>, <c>azure-blob</c>) without breaking the wire shape.
/// </summary>
public sealed class ChannelAttachmentStorageRef
{
    public required string Kind { get; init; }

    public required Guid Id { get; init; }

    /// <summary>Cached size in bytes; serves UI without an extra round trip to the blob store.</summary>
    public long? SizeBytes { get; init; }
}

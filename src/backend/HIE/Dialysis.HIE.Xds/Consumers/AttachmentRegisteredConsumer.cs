using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIE.Xds.Domain;
using Dialysis.HIE.Xds.Ports;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Xds.Consumers;

/// <summary>
/// Bridges SmartConnect attachment writes into the IHE XDS Registry. Subscribes to
/// <see cref="AttachmentRegisteredIntegrationEvent"/> over Transponder; for each event, builds an
/// XDS <see cref="DocumentEntry"/> from the attachment metadata and submits it via
/// <see cref="IXdsRegistry.RegisterAsync"/> (ITI-42).
/// </summary>
/// <remarks>
/// The bytes themselves stay in SmartConnect's blob store — the XDS Registry holds a reference
/// (via <see cref="DocumentEntry.RepositoryUniqueId"/> + <see cref="DocumentEntry.UniqueId"/>),
/// and the SmartConnect signed-URL factory mints time-bounded fetch URLs when consumers query.
/// This keeps a single source of truth for attachment bytes while letting partner organizations
/// discover them through the standard XDS query path.
/// </remarks>
public sealed class AttachmentRegisteredConsumer(
    IXdsRegistry registry,
    ILogger<AttachmentRegisteredConsumer> logger)
    : IConsumer<AttachmentRegisteredIntegrationEvent>
{
    private const string SmartConnectRepositoryId = "urn:dialysis:smartconnect:attachments";

    public async Task HandleAsync(ConsumeContext<AttachmentRegisteredIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        var entry = new DocumentEntry(
            UniqueId: msg.AttachmentId.ToString("D"),
            PatientId: msg.PatientId ?? "anonymous",
            MimeType: msg.MimeType,
            // FormatCode: opaque XDS terminology — defaulting to "scanned document" until a real
            // mapping from MIME → IHE FormatCode is wired in. For now the consumer is a stub that
            // demonstrates the integration shape rather than full terminology coverage.
            FormatCode: "urn:ihe:iti:xds-sd:pdf:2008",
            ClassCode: "Other",
            TypeCode: "Other",
            ConfidentialityCode: "N",
            SourceOrgId: msg.SourceOrgId ?? "smartconnect",
            CreationTime: msg.OccurredOn,
            Title: null,
            RepositoryUniqueId: SmartConnectRepositoryId,
            Size: msg.SizeBytes);

        var submission = new SubmissionSet(
            UniqueId: Guid.NewGuid().ToString(),
            PatientId: entry.PatientId,
            SourceId: entry.SourceOrgId,
            SubmissionTime: msg.OccurredOn,
            DocumentUniqueIds: [entry.UniqueId]);

        await registry.RegisterAsync(submission, [entry], context.CancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Registered attachment {AttachmentId} in XDS for patient {PatientId}",
            msg.AttachmentId, entry.PatientId);
    }
}

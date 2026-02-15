using Dialysis.Contracts.Messages;
using Dialysis.HisIntegration.Features.Hl7Streaming;
using Dialysis.Messaging;
using Intercessor.Abstractions;

namespace Dialysis.HisIntegration.Workers;

/// <summary>
/// Handles Hl7Ingested messages from Transponder (Azure Service Bus hl7-ingest topic)
/// and dispatches to the Hl7StreamIngest pipeline.
/// </summary>
public sealed class Hl7IngestedHandler : IMessageHandler<Hl7Ingested>
{
    private readonly ISender _sender;

    public Hl7IngestedHandler(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task HandleAsync(Hl7Ingested message, CancellationToken cancellationToken = default)
        => _sender.SendAsync(new Hl7StreamIngestCommand
        {
            RawMessage = message.RawMessage,
            MessageType = message.MessageType,
            TenantId = message.TenantId
        }, cancellationToken);
}

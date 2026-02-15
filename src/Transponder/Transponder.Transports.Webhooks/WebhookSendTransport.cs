using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Webhooks;

internal sealed class WebhookSendTransport : ISendTransport
{
    private readonly Uri _destination;

    public WebhookSendTransport(Uri destination)
    {
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    public Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            $"Webhook transport is publish-only. Send is not supported for destination '{_destination}'.");
}

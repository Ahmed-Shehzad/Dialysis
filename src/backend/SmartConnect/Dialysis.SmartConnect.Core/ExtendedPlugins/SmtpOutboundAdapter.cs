using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>Sends payload as mail body via SMTP (parameters JSON: Host, Port, From, To, Subject).</summary>
public sealed class SmtpOutboundAdapter : IOutboundAdapter
{
    public string Kind => "smtp";

    public async Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(HttpOutboundAdapter.ParametersMetadataKey, out var json) ||
            string.IsNullOrWhiteSpace(json))
        {
            return new OutboundSendResult(false, "SMTP outbound requires parameters JSON.");
        }

        var opts = JsonSerializer.Deserialize<SmtpOutboundParameters>(json);
        if (opts is null ||
            string.IsNullOrWhiteSpace(opts.Host) ||
            string.IsNullOrWhiteSpace(opts.From) ||
            string.IsNullOrWhiteSpace(opts.To))
        {
            return new OutboundSendResult(false, "SMTP parameters require Host, From, and To.");
        }

        try
        {
            var body = Encoding.UTF8.GetString(message.Payload.Span);
            using var mail = new MailMessage(opts.From, opts.To, opts.Subject ?? "", body);
            using var client = new SmtpClient(opts.Host, opts.Port);
            await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
            return new OutboundSendResult(true, null);
        }
        catch (Exception ex)
        {
            return new OutboundSendResult(false, ex.Message);
        }
    }
}

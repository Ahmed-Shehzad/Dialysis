using System.Net.Mail;
using System.Text.Json;

namespace Dialysis.SmartConnect.Alerts.Actions;

/// <summary>
/// Sends an SMTP email when an alert fires. Properties JSON shape:
/// <c>{"host":"smtp.example","port":587,"from":"...","to":"a@b,c@d","subject":"...","body":"..."}</c>.
/// <see cref="AlertVariables"/> is applied to <c>subject</c> and <c>body</c>.
/// </summary>
public sealed class EmailAlertActionProvider : IAlertActionProvider
{
    public const string KindValue = "email";

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public string Kind => KindValue;

    /// <summary>Test seam: override to inject a fake <see cref="ISmtpDeliverer"/>.</summary>
    public ISmtpDeliverer Deliverer { get; init; } = new DefaultSmtpDeliverer();

    public async Task<AlertActionResult> ExecuteAsync(
        AlertEvent evt,
        AlertRule rule,
        AlertActionSlot slot,
        CancellationToken cancellationToken)
    {
        EmailProperties? props;
        try
        {
            props = string.IsNullOrWhiteSpace(slot.PropertiesJson)
                ? null
                : JsonSerializer.Deserialize<EmailProperties>(slot.PropertiesJson, _jsonOpts);
        }
        catch (JsonException ex)
        {
            return AlertActionResult.Failure($"Invalid email action properties JSON: {ex.Message}");
        }
        if (props is null || string.IsNullOrWhiteSpace(props.Host) || string.IsNullOrWhiteSpace(props.From) || string.IsNullOrWhiteSpace(props.To))
        {
            return AlertActionResult.Failure("Email action requires 'host', 'from', and 'to' properties.");
        }

        var subject = AlertVariables.Render(props.Subject ?? $"SmartConnect alert: {rule.Name}", evt, rule);
        var body = AlertVariables.Render(props.Body ?? props.Subject ?? rule.Name, evt, rule);
        var port = props.Port > 0 ? props.Port : 25;

        using var message = new MailMessage(props.From, props.To, subject, body);
        try
        {
            await Deliverer.SendAsync(message, props.Host, port, cancellationToken).ConfigureAwait(false);
            return AlertActionResult.Success($"Sent to {props.To}");
        }
        catch (Exception ex)
        {
            return AlertActionResult.Failure(ex.Message);
        }
    }

    private sealed class EmailProperties
    {
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }

    /// <summary>Indirection so tests can substitute a fake deliverer instead of opening a real SMTP socket.</summary>
    public interface ISmtpDeliverer
    {
        Task SendAsync(MailMessage message, string host, int port, CancellationToken cancellationToken);
    }

    private sealed class DefaultSmtpDeliverer : ISmtpDeliverer
    {
        public async Task SendAsync(MailMessage message, string host, int port, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient(host, port);
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}

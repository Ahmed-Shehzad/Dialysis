using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace Dialysis.SmartConnect.Alerts.Actions;

/// <summary>
/// Sends an SMTP email when an alert fires. Properties JSON shape:
/// <c>{"host":"smtp.example","port":587,"from":"...","to":"a@b,c@d","subject":"...","body":"...",
/// "enableSsl":true,"username":"...","password":"...","timeoutSeconds":30}</c>.
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
        var transport = new SmtpTransportOptions(
            Host: props.Host,
            Port: port,
            EnableSsl: props.EnableSsl,
            Username: props.Username,
            Password: props.Password,
            TimeoutSeconds: props.TimeoutSeconds);
        try
        {
            await Deliverer.SendAsync(message, transport, cancellationToken).ConfigureAwait(false);
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
        public bool EnableSsl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int TimeoutSeconds { get; set; }
    }

    /// <summary>Per-message SMTP transport settings: host, port, TLS, optional credentials, timeout.</summary>
    public sealed record SmtpTransportOptions
    {
        /// <summary>Per-message SMTP transport settings: host, port, TLS, optional credentials, timeout.</summary>
        public SmtpTransportOptions(string Host,
            int Port,
            bool EnableSsl,
            string? Username,
            string? Password,
            int TimeoutSeconds)
        {
            this.Host = Host;
            this.Port = Port;
            this.EnableSsl = EnableSsl;
            this.Username = Username;
            this.Password = Password;
            this.TimeoutSeconds = TimeoutSeconds;
        }
        public string Host { get; init; }
        public int Port { get; init; }
        public bool EnableSsl { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
        public int TimeoutSeconds { get; init; }
        public void Deconstruct(out string Host, out int Port, out bool EnableSsl, out string? Username, out string? Password, out int TimeoutSeconds)
        {
            Host = this.Host;
            Port = this.Port;
            EnableSsl = this.EnableSsl;
            Username = this.Username;
            Password = this.Password;
            TimeoutSeconds = this.TimeoutSeconds;
        }
    }

    /// <summary>Indirection so tests can substitute a fake deliverer instead of opening a real SMTP socket.</summary>
    public interface ISmtpDeliverer
    {
        Task SendAsync(MailMessage message, SmtpTransportOptions transport, CancellationToken cancellationToken);
    }

    private sealed class DefaultSmtpDeliverer : ISmtpDeliverer
    {
        public async Task SendAsync(MailMessage message, SmtpTransportOptions transport, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient(transport.Host, transport.Port)
            {
                EnableSsl = transport.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };
            if (transport.TimeoutSeconds > 0)
            {
                client.Timeout = transport.TimeoutSeconds * 1000;
            }
            if (!string.IsNullOrEmpty(transport.Username))
            {
                client.Credentials = new NetworkCredential(transport.Username, transport.Password ?? string.Empty);
            }
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}

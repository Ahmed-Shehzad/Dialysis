namespace Dialysis.SmartConnect.ExtendedPlugins;

internal sealed class SmtpOutboundParameters
{
    public string? Host { get; set; }

    public int Port { get; set; } = 25;

    public string? From { get; set; }

    public string? To { get; set; }

    public string? Subject { get; set; }
}

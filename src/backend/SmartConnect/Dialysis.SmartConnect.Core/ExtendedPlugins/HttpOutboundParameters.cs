namespace Dialysis.SmartConnect.ExtendedPlugins;

internal sealed class HttpOutboundParameters
{
    public string? Url { get; set; }

    public string? Method { get; set; }

    public Dictionary<string, string>? Headers { get; set; }
}

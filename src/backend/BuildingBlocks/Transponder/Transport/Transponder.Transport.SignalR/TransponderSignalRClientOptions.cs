namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Client connection to a <see cref="TransponderSignalRHub"/> ingress host.</summary>
public sealed class TransponderSignalRClientOptions
{
    /// <summary>Full hub URL (e.g. https://localhost:5001/hubs/transponder).</summary>
    public string HubUrl { get; set; } = string.Empty;

    /// <summary>Optional bearer token factory for authenticated hubs.</summary>
    public Func<Task<string?>>? AccessTokenProvider { get; set; }
}

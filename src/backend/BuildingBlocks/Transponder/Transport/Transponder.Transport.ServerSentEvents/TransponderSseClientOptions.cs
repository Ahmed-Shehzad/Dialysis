namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>HTTP client for the SSE ingress relay (POST + GET).</summary>
public sealed class TransponderSseClientOptions
{
    /// <summary>Base URL with trailing slash (e.g. <c>https://localhost:5001/</c>).</summary>
    public string BaseAddress { get; set; } = string.Empty;

    /// <summary>Relative path for POST publish (default <c>transponder/sse/publish</c>).</summary>
    public string PublishPath { get; set; } = "transponder/sse/publish";

    /// <summary>Relative path for GET subscribe (default <c>transponder/sse/subscribe</c>).</summary>
    public string SubscribePath { get; set; } = "transponder/sse/subscribe";

    /// <summary>Optional bearer token factory for authenticated ingress.</summary>
    public Func<Task<string?>>? AccessTokenProvider { get; set; }
}

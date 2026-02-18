using Transponder.Transports.Abstractions;
using Transponder.Transports.SSE.Abstractions;

namespace Transponder.Transports.SSE;

/// <summary>
/// Factory for SSE transport hosts.
/// </summary>
public sealed class SseTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["sse"];

    public string Name => "SSE";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not ISseHostSettings sseSettings
            ? throw new ArgumentException(
                $"Expected {nameof(ISseHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : new SseTransportHost(sseSettings);
    }
}

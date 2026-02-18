using Transponder.Transports.Abstractions;
using Transponder.Transports.Grpc.Abstractions;

namespace Transponder.Transports.Grpc;

/// <summary>
/// Factory for gRPC transport hosts.
/// </summary>
public sealed class GrpcTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["http", "https", "grpc", "grpcs"];

    public string Name => "Grpc";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not IGrpcHostSettings grpcSettings
            ? throw new ArgumentException(
                $"Expected {nameof(IGrpcHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : (ITransportHost)new GrpcTransportHost(grpcSettings);
    }
}

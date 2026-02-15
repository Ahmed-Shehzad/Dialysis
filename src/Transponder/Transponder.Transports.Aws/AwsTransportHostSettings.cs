using Transponder.Transports.Abstractions;
using Transponder.Transports.Aws.Abstractions;

namespace Transponder.Transports.Aws;

/// <summary>
/// Default AWS transport host settings.
/// </summary>
public sealed class AwsTransportHostSettings : TransportHostSettings, IAwsTransportHostSettings
{
    public AwsTransportHostSettings(
        Uri address,
        string region,
        IAwsTopology? topology = null,
        bool useTls = true,
        string? accessKeyId = null,
        string? secretAccessKey = null,
        string? sessionToken = null,
        string? serviceUrl = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Region must be provided.", nameof(region));

        Region = region;
        Topology = topology ?? new AwsTopology();
        UseTls = useTls;
        AccessKeyId = accessKeyId;
        SecretAccessKey = secretAccessKey;
        SessionToken = sessionToken;
        ServiceUrl = serviceUrl;
    }

    public IAwsTopology Topology { get; }

    public string Region { get; }

    public string? AccessKeyId { get; }

    public string? SecretAccessKey { get; }

    public string? SessionToken { get; }

    public string? ServiceUrl { get; }

    public bool UseTls { get; }
}

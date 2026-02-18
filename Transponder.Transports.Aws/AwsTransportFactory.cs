using Transponder.Transports.Abstractions;
using Transponder.Transports.Aws.Abstractions;

namespace Transponder.Transports.Aws;

/// <summary>
/// Factory for AWS transport hosts.
/// </summary>
public sealed class AwsTransportFactory : ITransportFactory
{
    private static readonly IReadOnlyCollection<string> Schemes =
        ["aws", "sqs", "sns"];

    public string Name => "AWS";

    public IReadOnlyCollection<string> SupportedSchemes => Schemes;

    public ITransportHost CreateHost(ITransportHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings is not IAwsTransportHostSettings awsSettings
            ? throw new ArgumentException(
                $"Expected {nameof(IAwsTransportHostSettings)} but received {settings.GetType().Name}.",
                nameof(settings))
            : (ITransportHost)new AwsTransportHost(awsSettings);
    }
}

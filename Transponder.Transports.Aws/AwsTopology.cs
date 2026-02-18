using Transponder.Transports.Aws.Abstractions;

namespace Transponder.Transports.Aws;

/// <summary>
/// Default AWS topology conventions.
/// </summary>
public sealed class AwsTopology : IAwsTopology
{
    public string GetQueueName(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return !string.IsNullOrWhiteSpace(address.AbsolutePath) && address.AbsolutePath != "/"
            ? address.AbsolutePath.Trim('/')
            : address.Host;
    }

    public string GetTopicName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return messageType.Name;
    }
}

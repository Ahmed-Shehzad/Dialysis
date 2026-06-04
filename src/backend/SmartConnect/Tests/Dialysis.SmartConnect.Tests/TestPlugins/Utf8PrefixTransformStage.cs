using System.Text;

namespace Dialysis.SmartConnect.Tests.TestPlugins;

/// <summary>Test-only transform that prepends a UTF-8 prefix to the payload.</summary>
public sealed class Utf8PrefixTransformStage : ITransformStage
{
    private readonly string _asciiPrefix;
    /// <summary>Test-only transform that prepends a UTF-8 prefix to the payload.</summary>
    public Utf8PrefixTransformStage(string asciiPrefix) => _asciiPrefix = asciiPrefix;
    public const string KindValue = "utf8-prefix-test";

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var prefix = Encoding.UTF8.GetBytes(_asciiPrefix);
        var body = message.Payload.ToArray();
        var combined = new byte[prefix.Length + body.Length];
        prefix.AsSpan().CopyTo(combined);
        body.AsSpan().CopyTo(combined.AsSpan(prefix.Length));
        return Task.FromResult(message.CloneWithPayload(combined, PayloadFormat.Utf8Text));
    }
}

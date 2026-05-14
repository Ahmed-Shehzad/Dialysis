using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Concurrent;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// XSLT 1.0 transform stage. Parameters JSON must include <c>stylesheet</c> (the XSLT markup).
/// Compiled transforms are cached by SHA-256 hash of the stylesheet content.
/// </summary>
public sealed class XsltTransformStage : ITransformStage
{
    public const string ParametersMetadataKey = "smartconnect.transform.parameters";

    private readonly ConcurrentDictionary<string, XslCompiledTransform> _cache = new();

    public string Kind => "xslt";

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(message);
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("stylesheet", out var stylesheetEl))
        {
            return Task.FromResult(message);
        }

        var stylesheet = stylesheetEl.GetString();
        if (string.IsNullOrWhiteSpace(stylesheet))
        {
            return Task.FromResult(message);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var transform = GetOrCompile(stylesheet!);

        using var inputStream = new MemoryStream(message.Payload.ToArray());
        using var xmlReader = XmlReader.Create(inputStream);
        using var outputStream = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(outputStream, transform.OutputSettings);
        transform.Transform(xmlReader, xmlWriter);
#pragma warning disable VSTHRD103 // XslCompiledTransform.OutputSettings.Async is false; FlushAsync would throw. Sink is MemoryStream, no I/O.
        xmlWriter.Flush();
#pragma warning restore VSTHRD103

        var resultBytes = outputStream.ToArray();
        return Task.FromResult(message.CloneWithPayload(resultBytes, PayloadFormat.Utf8Text));
    }

    private XslCompiledTransform GetOrCompile(string stylesheet)
    {
        var hash = ComputeHash(stylesheet);
        return _cache.GetOrAdd(hash, _ => Compile(stylesheet));
    }

    private static XslCompiledTransform Compile(string stylesheet)
    {
        var xslt = new XslCompiledTransform();
        using var reader = XmlReader.Create(new StringReader(stylesheet));
        xslt.Load(reader, XsltSettings.Default, new XmlUrlResolver());
        return xslt;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

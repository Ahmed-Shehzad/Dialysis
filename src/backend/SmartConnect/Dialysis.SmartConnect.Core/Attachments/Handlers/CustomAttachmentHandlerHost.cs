using System.Text.Json;

namespace Dialysis.SmartConnect.Attachments.Handlers;

/// <summary>
/// Routes <c>custom</c>-kind slots to a plug-in author's registered handler. The slot's
/// <see cref="AttachmentHandlerContext.PropertiesJson"/> must include <c>"customKind": "&lt;suffix&gt;"</c>;
/// the host looks up <c>custom:&lt;suffix&gt;</c> in <see cref="IFlowPluginRegistry"/>. Mirth UG p226
/// "Custom Attachment Handler".
/// </summary>
public sealed class CustomAttachmentHandlerHost : IAttachmentHandler
{
    private readonly Func<IFlowPluginRegistry> _registryAccessor;
    /// <summary>
    /// Routes <c>custom</c>-kind slots to a plug-in author's registered handler. The slot's
    /// <see cref="AttachmentHandlerContext.PropertiesJson"/> must include <c>"customKind": "&lt;suffix&gt;"</c>;
    /// the host looks up <c>custom:&lt;suffix&gt;</c> in <see cref="IFlowPluginRegistry"/>. Mirth UG p226
    /// "Custom Attachment Handler".
    /// </summary>
    public CustomAttachmentHandlerHost(Func<IFlowPluginRegistry> registryAccessor) => _registryAccessor = registryAccessor;
    public const string KindValue = "custom";
    public const string CustomKindPrefix = "custom:";

    public string Kind => KindValue;

    public async Task<AttachmentHandlerResult> ExtractAsync(
        IntegrationMessage message,
        AttachmentHandlerContext context,
        CancellationToken cancellationToken)
    {
        var suffix = ExtractCustomKindSuffix(context.PropertiesJson);
        if (string.IsNullOrWhiteSpace(suffix))
        {
            return AttachmentHandlerResult.Unchanged(message.Payload);
        }

        var fullKind = $"{CustomKindPrefix}{suffix}";
        var handler = _registryAccessor().TryResolveAttachmentHandler(fullKind);
        if (handler is null)
        {
            return AttachmentHandlerResult.Unchanged(message.Payload);
        }

        return await handler.ExtractAsync(message, context, cancellationToken).ConfigureAwait(false);
    }

    private static string? ExtractCustomKindSuffix(string? propertiesJson)
    {
        if (string.IsNullOrWhiteSpace(propertiesJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(propertiesJson);
            if (doc.RootElement.TryGetProperty("customKind", out var el)
                && el.ValueKind == JsonValueKind.String)
            {
                return el.GetString();
            }
        }
        catch (JsonException) { /* malformed properties JSON → no custom kind */ }
        return null;
    }
}

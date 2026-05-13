using System.Text;
using Dialysis.SmartConnect.Attachments;
using Jint;
using Jint.Native;

namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Binds the JS <c>addAttachment(data, type) -&gt; "${ATTACH:&lt;id&gt;}"</c> global. The bound function
/// persists the bytes via <see cref="IAttachmentStore"/> and returns the inline reference token, mirroring
/// Mirth's <c>addAttachment</c> API (UG p226). The supplied <paramref name="store"/> and
/// <paramref name="messageId"/>/<paramref name="flowId"/> are captured per Jint engine; callers pass a
/// no-op delegate by passing <c>null</c> for <paramref name="store"/>.
/// </summary>
internal static class AttachmentJsBinder
{
    public static void Bind(
        Engine engine,
        IAttachmentStore? store,
        Guid flowId,
        Guid messageId,
        string defaultMimeType,
        CancellationToken cancellationToken)
    {
        if (store is null || messageId == Guid.Empty)
        {
            engine.SetValue("addAttachment", new Func<JsValue, JsValue, string>((_, _) => string.Empty));
            return;
        }

        engine.SetValue("addAttachment", new Func<JsValue, JsValue, string>((data, type) =>
        {
            var bytes = JsValueToBytes(data);
            var mime = type.IsUndefined() || type.IsNull() ? defaultMimeType : type.AsString();
            if (string.IsNullOrWhiteSpace(mime)) mime = "application/octet-stream";

            var attachment = new Attachment
            {
                Id = Guid.CreateVersion7(),
                MessageId = messageId,
                FlowId = flowId,
                MimeType = mime,
                Data = bytes,
                SizeBytes = bytes.LongLength,
                CreatedUtc = DateTimeOffset.UtcNow,
            };

            var stored = store.AddAsync(attachment, cancellationToken).GetAwaiter().GetResult();
            return AttachmentReference.Format(stored.Id);
        }));
    }

    private static byte[] JsValueToBytes(JsValue value)
    {
        if (value.IsString()) return Encoding.UTF8.GetBytes(value.AsString());
        if (value.IsArray())
        {
            var arr = value.AsArray();
            var len = (int)arr.Length;
            var bytes = new byte[len];
            for (var i = 0; i < len; i++)
            {
                var v = arr[(uint)i];
                bytes[i] = (byte)(v.IsNumber() ? (byte)v.AsNumber() : 0);
            }
            return bytes;
        }
        if (value.IsObject()) return Encoding.UTF8.GetBytes(value.ToString());
        throw new ArgumentException("addAttachment expects a string or byte-array data argument.");
    }
}

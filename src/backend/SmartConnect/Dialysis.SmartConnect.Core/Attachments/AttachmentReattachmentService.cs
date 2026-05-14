using System.Text;

namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Inflates <c>${ATTACH:&lt;id&gt;}</c> tokens in an outbound payload back to the stored bytes.
/// Invoked from <c>FlowRuntimeEngine</c> immediately before a route's <c>SendAsync</c> when that route
/// has <see cref="OutboundRouteSlot.ReattachAttachments"/> = true.
/// </summary>
public sealed class AttachmentReattachmentService(IAttachmentStore store)
{
    public async Task<ReadOnlyMemory<byte>> InflateAsync(
        ReadOnlyMemory<byte> payload,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(payload.Span);
        if (text.IndexOf(AttachmentReference.Prefix, StringComparison.Ordinal) < 0)
        {
            return payload;
        }

        var tokens = AttachmentReference.Scan(text).ToList();
        if (tokens.Count == 0) return payload;

        // Resolve all referenced attachments up front; missing ones leave their tokens intact.
        var resolved = new Dictionary<Guid, Attachment>(tokens.Count);
        foreach (var (_, _, id) in tokens)
        {
            if (resolved.ContainsKey(id)) continue;
            var att = await store.GetAsync(id, cancellationToken).ConfigureAwait(false);
            if (att is not null) resolved[id] = att;
        }

        // Single-pass rebuild: copy non-token text verbatim; for each matched token whose attachment exists,
        // emit raw bytes. Build the result as a byte stream because attachments may not be valid UTF-8.
        using var ms = new MemoryStream(payload.Length);
        var cursor = 0;
        var utf8 = Encoding.UTF8;

        foreach (var (start, length, id) in tokens)
        {
            if (!resolved.TryGetValue(id, out var att)) continue;

            if (start > cursor)
            {
                var span = text.AsSpan(cursor, start - cursor);
                var pre = utf8.GetBytes(span.ToArray());
                await ms.WriteAsync(pre.AsMemory(0, pre.Length), cancellationToken).ConfigureAwait(false);
            }
            ms.Write(att.Data.Span);
            cursor = start + length;
        }

        if (cursor < text.Length)
        {
            var tail = utf8.GetBytes(text.AsSpan(cursor).ToArray());
            await ms.WriteAsync(tail.AsMemory(0, tail.Length), cancellationToken).ConfigureAwait(false);
        }

        return new ReadOnlyMemory<byte>(ms.ToArray());
    }
}

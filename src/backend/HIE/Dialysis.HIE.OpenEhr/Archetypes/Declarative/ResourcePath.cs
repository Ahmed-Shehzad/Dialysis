using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Dialysis.HIE.OpenEhr.Archetypes.Declarative;

/// <summary>
/// Walks a dotted path expression over a FHIR resource POCO via reflection. Supports the
/// minimum surface a clinical archetype mapping needs without pulling in a FHIRPath engine:
///
/// <list type="bullet">
///   <item><c>Code.Coding[0].Code</c> — list indexing.</item>
///   <item><c>Note[?].Text</c> — wildcard iteration; produces an <see cref="IReadOnlyList{T}"/> result.</item>
///   <item><c>Value as Quantity.Value</c> — subtype cast (returns <c>null</c> when the runtime type
///         doesn't match, so a choice-type like <c>value[x]</c> can be declared multiple times in
///         the mapping definition without a runtime cast exception).</item>
///   <item><c>Status</c> — primitive / enum; enums are emitted as their string name to match the
///         existing hand-rolled projections.</item>
/// </list>
/// Tokens are PascalCase FHIR property names so the JSON definitions read like the
/// equivalent C# expression a maintainer would otherwise write. Token segments are split on
/// <c>.</c>; sub-expressions inside brackets / casts are atomic.
/// </summary>
public static class ResourcePath
{
    /// <summary>
    /// Evaluates <paramref name="path"/> against <paramref name="source"/>. Returns either a
    /// single value, an <see cref="IReadOnlyList{T}"/> (when a <c>[?]</c> wildcard was used),
    /// or <c>null</c> when any traversal step missed.
    /// </summary>
    public static object? Evaluate(object? source, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (source is null)
            return null;

        object? current = source;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current is null)
                return null;
            // When the running value is a list (produced by an earlier `[?]`), each segment
            // applies element-wise and the result is flattened — turning `Interpretation[?].Coding[?].Code`
            // into a flat string list rather than a list-of-lists.
            if (current is IReadOnlyList<object?> elements)
            {
                var mapped = new List<object?>(elements.Count);
                foreach (var element in elements)
                {
                    if (element is null)
                        continue;
                    var step = ApplySegment(element, segment);
                    if (step is IReadOnlyList<object?> sublist)
                    {
                        foreach (var sub in sublist)
                            if (sub is not null)
                                mapped.Add(sub);
                    }
                    else if (step is not null)
                    {
                        mapped.Add(step);
                    }
                }
                current = mapped;
                continue;
            }
            current = ApplySegment(current, segment);
        }
        return NormalizeLeaf(current);
    }

    private static object? ApplySegment(object current, string segment)
    {
        var (propertyName, op) = ParseSegment(segment);

        // `as Type` cast — narrows a choice-type field to the requested concrete type.
        if (op is { Kind: SegmentOpKind.Cast } castOp)
        {
            var property = ResolveProperty(current.GetType(), propertyName);
            if (property is null)
                return null;
            var value = property.GetValue(current);
            if (value is null)
                return null;
            var typeName = castOp.Argument;
            return value.GetType().Name == typeName ? value : null;
        }

        // Wildcard expansion — if we're already on a list and the segment is `[?]`, the previous
        // pipeline already produced a list; here we need to flatten one more level.
        var resolved = ResolveProperty(current.GetType(), propertyName);
        if (resolved is null)
            return null;
        var raw = resolved.GetValue(current);

        return op switch
        {
            null => raw,
            { Kind: SegmentOpKind.Index, IndexValue: var i } => GetIndexed(raw, i),
            { Kind: SegmentOpKind.Wildcard } => ExpandWildcard(raw),
            _ => raw,
        };
    }

    private static object? GetIndexed(object? collection, int index)
    {
        if (collection is null)
            return null;
        if (collection is IList list)
        {
            return index >= 0 && index < list.Count ? list[index] : null;
        }
        if (collection is IEnumerable enumerable)
        {
            var i = 0;
            foreach (var item in enumerable)
            {
                if (i++ == index)
                    return item;
            }
        }
        return null;
    }

    private static IReadOnlyList<object?> ExpandWildcard(object? collection)
    {
        if (collection is null)
            return [];
        if (collection is IEnumerable enumerable and not string)
        {
            var result = new List<object?>();
            foreach (var item in enumerable)
                result.Add(item);
            return result;
        }
        return [collection];
    }

    private static (string PropertyName, SegmentOp? Op) ParseSegment(string segment)
    {
        // Pattern: "Name", "Name[0]", "Name[?]", "Name as Type"
        var bracketIndex = segment.IndexOf('[', StringComparison.Ordinal);
        if (bracketIndex >= 0)
        {
            var closing = segment.IndexOf(']', bracketIndex);
            if (closing < 0)
                throw new FormatException($"Unterminated bracket in '{segment}'.");
            var name = segment[..bracketIndex];
            var token = segment.Substring(bracketIndex + 1, closing - bracketIndex - 1);
            if (token == "?")
                return (name, new SegmentOp(SegmentOpKind.Wildcard, 0, null));
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
            {
                return (name, new SegmentOp(SegmentOpKind.Index, idx, null));
            }
            throw new FormatException($"Unrecognised index token '{token}' in '{segment}'.");
        }

        var asIndex = segment.IndexOf(" as ", StringComparison.Ordinal);
        if (asIndex > 0)
        {
            var name = segment[..asIndex];
            var typeName = segment[(asIndex + 4)..].Trim();
            return (name, new SegmentOp(SegmentOpKind.Cast, 0, typeName));
        }

        return (segment, null);
    }

    private static PropertyInfo? ResolveProperty(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    /// <summary>
    /// Final-step normalization: convert enums to their string name (matching the existing
    /// hand-rolled projections that called <c>.ToString()</c>), and turn empty wildcard lists
    /// into <c>null</c> so the JSON output skips the key.
    /// </summary>
    private static object? NormalizeLeaf(object? value)
    {
        if (value is null)
            return null;
        if (value is IReadOnlyList<object?> list)
        {
            if (list.Count == 0)
                return null;
            var normalized = new List<object?>(list.Count);
            foreach (var element in list)
            {
                var normalizedElement = NormalizeLeaf(element);
                if (normalizedElement is not null)
                    normalized.Add(normalizedElement);
            }
            return normalized.Count == 0 ? null : normalized;
        }
        if (value is Enum)
            return value.ToString();
        return value;
    }

    private enum SegmentOpKind { Index, Wildcard, Cast }
    private sealed record SegmentOp
    {
        public SegmentOp(SegmentOpKind Kind, int IndexValue, string? Argument)
        {
            this.Kind = Kind;
            this.IndexValue = IndexValue;
            this.Argument = Argument;
        }
        public SegmentOpKind Kind { get; init; }
        public int IndexValue { get; init; }
        public string? Argument { get; init; }
        public void Deconstruct(out SegmentOpKind Kind, out int IndexValue, out string? Argument)
        {
            Kind = this.Kind;
            IndexValue = this.IndexValue;
            Argument = this.Argument;
        }
    }
}

namespace Dialysis.SmartConnect.DataTypes;

/// <summary>
/// Parses raw message bytes into a structured <see cref="ParsedMessage"/> for field-level access.
/// </summary>
public interface IDataTypeParser
{
    /// <summary>Data type this parser handles (e.g. <c>"hl7v2"</c>, <c>"xml"</c>, <c>"json"</c>).</summary>
    string DataType { get; }

    /// <summary>Parse <paramref name="payload"/> into a queryable structure.</summary>
    ParsedMessage Parse(ReadOnlySpan<byte> payload);
}

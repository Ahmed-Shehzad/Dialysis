namespace Dialysis.SmartConnect.DataTypes;

/// <summary>
/// Parsed message providing field-level access via path syntax.
/// </summary>
public abstract class ParsedMessage
{
    /// <summary>Data type key (matches <see cref="IDataTypeParser.DataType"/>).</summary>
    public abstract string DataType { get; }

    /// <summary>
    /// Retrieves a value by path syntax (implementation-specific).
    /// Returns <c>null</c> when path is not found.
    /// </summary>
    public abstract string? GetValue(string path);

    /// <summary>
    /// Sets a value at a given path (implementation-specific). Returns the modified message.
    /// </summary>
    public abstract ParsedMessage SetValue(string path, string value);

    /// <summary>Serializes back to the wire format.</summary>
    public abstract string Serialize();
}

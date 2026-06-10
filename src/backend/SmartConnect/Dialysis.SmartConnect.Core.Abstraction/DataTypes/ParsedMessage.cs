namespace Dialysis.SmartConnect.DataTypes;

/// <summary>
/// Parsed message providing field-level access via path syntax.
/// </summary>
// Kept an abstract class (not an interface) so shared path-syntax helpers can be added
// without breaking implementors; it is also a closed hierarchy by design.
#pragma warning disable S1694
public abstract class ParsedMessage
#pragma warning restore S1694
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

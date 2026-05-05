using System.Text;

namespace Dialysis.SmartConnect.DataTypes;

/// <summary>
/// Pure C# HL7 v2.x pipe-delimited parser.
/// Path syntax: <c>MSH.9</c> (field), <c>PID.3.1</c> (component), <c>PID.3.1.2</c> (subcomponent),
/// <c>PID.3[2]</c> (repeat index, 1-based).
/// </summary>
public sealed class Hl7V2Parser : IDataTypeParser
{
    public string DataType => "hl7v2";

    public ParsedMessage Parse(ReadOnlySpan<byte> payload)
    {
        var text = Encoding.UTF8.GetString(payload);
        return Hl7V2Message.Parse(text);
    }
}

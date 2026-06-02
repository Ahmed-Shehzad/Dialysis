namespace Dialysis.SmartConnect;

/// <summary>
/// Declared encoding for an <see cref="IntegrationMessage"/> payload.
/// </summary>
public enum PayloadFormat
{
    PlainText = 0,
    Utf8Text = 1,
    Binary = 2,
    Json = 3,

    /// <summary>ANSI ASC X12N — EDI 837 / 835 / 999 / 277CA pharmacy and billing transactions.</summary>
    AscX12 = 4,
}

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
}

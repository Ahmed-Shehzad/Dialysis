namespace Transponder.Transports.SSE.Abstractions;

/// <summary>
/// Provides SSE topology conventions.
/// </summary>
public interface ISseTopology
{
    /// <summary>
    /// Gets the SSE stream path.
    /// </summary>
    string StreamPath { get; }

    /// <summary>
    /// Gets the path used to send request messages.
    /// </summary>
    string SendPath { get; }

    /// <summary>
    /// Gets the path used to publish messages.
    /// </summary>
    string PublishPath { get; }
}

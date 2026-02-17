using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Hl7.Stream;

/// <summary>
/// Maps HTTP request to command and returns result. Vertical slice entry point for HL7 stream.
/// </summary>
public static class Hl7StreamEndpoint
{
    public static async Task<Hl7StreamResponse> HandleAsync(
        Hl7StreamRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ProcessHl7StreamCommand(request.RawMessage);
        return await sender.SendAsync(command, cancellationToken);
    }
}

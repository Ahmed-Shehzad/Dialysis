namespace Transponder;

public sealed record RemoteAddressResolution(
    IReadOnlyList<Uri> Addresses,
    RemoteAddressStrategy Strategy);

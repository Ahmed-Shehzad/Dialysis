namespace Transponder;

public sealed class RemoteAddressStrategySettings
{
    public string? Url { get; set; }
    public RemoteAddressStrategy RemoteAddressStrategy { get; set; } = RemoteAddressStrategy.PerDestinationHost;
}

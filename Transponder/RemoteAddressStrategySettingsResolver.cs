namespace Transponder;

public static class RemoteAddressStrategySettingsResolver
{
    public static RemoteAddressResolution Resolve(
        IReadOnlyList<RemoteAddressStrategySettings> entries,
        RemoteAddressStrategy? fallbackStrategy,
        string fallbackRemoteAddress)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (string.IsNullOrWhiteSpace(fallbackRemoteAddress))
            throw new ArgumentException("Fallback remote address is required.", nameof(fallbackRemoteAddress));

        var addresses = new List<Uri>();

        foreach (RemoteAddressStrategySettings entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Url)) continue;
            addresses.Add(new Uri(entry.Url));
        }

        if (addresses.Count == 0) addresses.Add(new Uri(fallbackRemoteAddress));

        bool hasEntries = entries.Any();
        bool hasRoundRobin = entries.Any(entry => entry.RemoteAddressStrategy == RemoteAddressStrategy.RoundRobin);

        RemoteAddressStrategy strategy = hasEntries
            ? StrategyFromEntries(hasRoundRobin)
            : fallbackStrategy ?? RemoteAddressStrategy.PerDestinationHost;



        return new RemoteAddressResolution(addresses, strategy);
    }

    private static RemoteAddressStrategy StrategyFromEntries(bool hasRoundRobin)
    {
        return hasRoundRobin
            ? RemoteAddressStrategy.RoundRobin
            : RemoteAddressStrategy.PerDestinationHost;
    }
}

namespace Transponder;

public sealed class TransponderSettings
{
    public string? LocalBaseAddress { get; set; }

    public string? RemoteBaseAddress { get; set; }

    public string? LocalServiceName { get; set; }

    public string? RemoteServiceName { get; set; }

    public RemoteAddressStrategy? RemoteAddressStrategy { get; set; }

    public RemoteAddressStrategySettings[] RemoteBaseAddresses { get; set; } = [];

    public (Uri LocalAddress, RemoteAddressResolution RemoteResolution) ResolveAddresses(
        string defaultLocalAddress,
        string defaultRemoteAddress)
    {
        if (string.IsNullOrWhiteSpace(defaultLocalAddress))
            throw new ArgumentException("Default local address is required.", nameof(defaultLocalAddress));

        if (string.IsNullOrWhiteSpace(defaultRemoteAddress))
            throw new ArgumentException("Default remote address is required.", nameof(defaultRemoteAddress));

        Uri localBaseAddress = ResolveLocalBaseAddress(defaultLocalAddress);
        Uri localAddress = localBaseAddress;

        string remoteFallback = GetRemoteFallbackAddress(localBaseAddress, defaultRemoteAddress);

        IReadOnlyList<RemoteAddressStrategySettings> entries = !string.IsNullOrWhiteSpace(RemoteServiceName)
            ? []
            : RemoteBaseAddresses ?? [];

        RemoteAddressResolution baseResolution = RemoteAddressStrategySettingsResolver.Resolve(
            entries,
            RemoteAddressStrategy,
            remoteFallback);

        IReadOnlyList<Uri> remoteAddresses = baseResolution.Addresses;

        var remoteResolution = new RemoteAddressResolution(remoteAddresses, baseResolution.Strategy);

        if (UsesHttp(localAddress) || remoteAddresses.Any(UsesHttp))
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        return (localAddress, remoteResolution);
    }

    public Uri ResolveLocalBaseAddress(string defaultLocalAddress)
    {
        if (string.IsNullOrWhiteSpace(defaultLocalAddress))
            throw new ArgumentException("Default local address is required.", nameof(defaultLocalAddress));

        var defaultLocalUri = new Uri(defaultLocalAddress);
        (string? scheme, int? port) = GetSchemeAndPort(defaultLocalUri);

        if (!string.IsNullOrWhiteSpace(LocalServiceName) && scheme is not null && port.HasValue)
            return new Uri($"{scheme}://{LocalServiceName}:{port.Value}");

        return !string.IsNullOrWhiteSpace(LocalBaseAddress) ? new Uri(LocalBaseAddress) : new Uri(defaultLocalAddress);
    }

    private string GetRemoteFallbackAddress(Uri localBaseAddress, string defaultRemoteAddress)
    {
        if (!string.IsNullOrWhiteSpace(RemoteServiceName))
            return $"{localBaseAddress.Scheme}://{RemoteServiceName}:{localBaseAddress.Port}";

        if (string.IsNullOrWhiteSpace(RemoteBaseAddress))
            return defaultRemoteAddress;

        return RemoteBaseAddress;
    }

    private static bool UsesHttp(Uri address) =>
        string.Equals(address.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

    private static (string? Scheme, int? Port) GetSchemeAndPort(Uri defaultLocalUri)
    {
        string? urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

        if (string.IsNullOrWhiteSpace(urls)) return (defaultLocalUri.Scheme, defaultLocalUri.Port);

        foreach (string rawUrl in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string parsedUrl = rawUrl.Replace("+", "localhost", StringComparison.Ordinal)
                .Replace("*", "localhost", StringComparison.Ordinal);

            if (Uri.TryCreate(parsedUrl, UriKind.Absolute, out Uri? uri))
                return (uri.Scheme, uri.Port);
        }

        return (defaultLocalUri.Scheme, defaultLocalUri.Port);
    }

}

using System.Text;

namespace Transponder;

/// <summary>
/// Builds request addresses using a convention-based path.
/// </summary>
public static class TransponderRequestAddressResolver
{
    public const string DefaultRequestPathPrefix = "requests";

    public sealed class RemoteAddressSelectionOptions
    {
        internal RemoteAddressStrategy Strategy { get; private set; } = RemoteAddressStrategy.PerDestinationHost;

        public void UsePerDestinationHost()
            => Strategy = RemoteAddressStrategy.PerDestinationHost;

        public void UseRoundRobin()
            => Strategy = RemoteAddressStrategy.RoundRobin;
    }

    public static Func<Type, Uri?> Create(
        IReadOnlyList<Uri> busAddresses,
        Action<RemoteAddressSelectionOptions>? configure = null,
        string? requestPathPrefix = null,
        Func<Type, string>? pathFormatter = null)
    {
        ArgumentNullException.ThrowIfNull(busAddresses);

        Uri[] addresses = [.. busAddresses.Where(address => address is not null)];
        if (addresses.Length == 0) throw new ArgumentException("At least one bus address is required.", nameof(busAddresses));

        var options = new RemoteAddressSelectionOptions();
        configure?.Invoke(options);

        string prefix = string.IsNullOrWhiteSpace(requestPathPrefix)
            ? DefaultRequestPathPrefix
            : requestPathPrefix.Trim('/');
        Func<Type, string> formatter = pathFormatter ?? DefaultPathFormatter;

        Func<Type, Uri?>[] resolvers = [.. addresses.Select(address => Create(address, prefix, formatter))];

        return options.Strategy == RemoteAddressStrategy.RoundRobin
            ? CreateRoundRobinResolver(resolvers)
            : CreateFixedResolver(resolvers[0]);
    }

    public static Func<Type, Uri?> Create(
        IReadOnlyList<Uri> busAddresses,
        RemoteAddressStrategy strategy,
        string? requestPathPrefix = null,
        Func<Type, string>? pathFormatter = null)
        => Create(
            busAddresses,
            options =>
            {
                if (strategy == RemoteAddressStrategy.RoundRobin) options.UseRoundRobin();
                else options.UsePerDestinationHost();
            },
            requestPathPrefix,
            pathFormatter);

    public static Func<Type, Uri?> Create(
        Uri busAddress,
        string? requestPathPrefix = null,
        Func<Type, string>? pathFormatter = null)
    {
        ArgumentNullException.ThrowIfNull(busAddress);

        string prefix = string.IsNullOrWhiteSpace(requestPathPrefix)
            ? DefaultRequestPathPrefix
            : requestPathPrefix.Trim('/');
        Func<Type, string> formatter = pathFormatter ?? DefaultPathFormatter;

        return messageType =>
        {
            string segment = formatter(messageType);
            if (string.IsNullOrWhiteSpace(segment)) return null;

            var builder = new UriBuilder(busAddress);
            string basePath = builder.Path?.TrimEnd('/') ?? string.Empty;
            builder.Path = $"{basePath}/{prefix}/{segment}";
            return builder.Uri;
        };
    }

    public static string DefaultPathFormatter(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        string raw = messageType.FullName ?? messageType.Name;
        return SanitizeSegment(raw);
    }

    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);

        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
            {
                _ = builder.Append(ch);
                continue;
            }

            if (ch is '.' or '+' or '/')
            {
                _ = builder.Append('-');
                continue;
            }

            _ = builder.Append('-');
        }

        string normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : Uri.EscapeDataString(normalized);
    }

    private static Func<Type, Uri?> CreateFixedResolver(Func<Type, Uri?> resolver)
        => resolver;

    private static Func<Type, Uri?> CreateRoundRobinResolver(Func<Type, Uri?>[] resolvers)
    {
        int counter = -1;

        return messageType =>
        {
            int index = NextIndex(resolvers.Length, ref counter);
            return resolvers[index](messageType);
        };
    }

    private static int NextIndex(int count, ref int counter)
    {
        int next = Interlocked.Increment(ref counter);
        int index = next % count;
        return index < 0 ? index + count : index;
    }
}

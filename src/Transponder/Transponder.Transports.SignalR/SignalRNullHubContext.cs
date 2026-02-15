using Microsoft.AspNetCore.SignalR;

namespace Transponder.Transports.SignalR;

internal sealed class SignalRNullHubContext : IHubContext<TransponderSignalRHub>
{
    public static readonly SignalRNullHubContext Instance = new();

    private SignalRNullHubContext()
    {
    }

    public IHubClients Clients { get; } = new NullHubClients();

    public IGroupManager Groups { get; } = new NullGroupManager();

    private sealed class NullHubClients : IHubClients
    {
        private static readonly IClientProxy Proxy = new NullClientProxy();

        public IClientProxy All => Proxy;

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;

        public IClientProxy Client(string connectionId) => Proxy;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;

        public IClientProxy Group(string groupName) => Proxy;

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;

        public IClientProxy User(string userId) => Proxy;

        public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class NullClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            => Task.FromException(new InvalidOperationException(
                "SignalR hub context is not available. Call services.AddSignalR() and register the hub."));
    }

    private sealed class NullGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
            => Task.FromException(new InvalidOperationException(
                "SignalR hub context is not available. Call services.AddSignalR() and register the hub."));

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
            => Task.FromException(new InvalidOperationException(
                "SignalR hub context is not available. Call services.AddSignalR() and register the hub."));
    }
}

using Microsoft.AspNetCore.SignalR;
using Shouldly;
using Xunit;

namespace Dialysis.Module.Bff.Events.Tests;

/// <summary>
/// Coverage for <see cref="SignalRBffNotifier"/>: pushes land on the correct patient/user group and
/// the guard clauses reject empty targets. Uses hand-rolled hub fakes (no mocking dependency).
/// </summary>
public sealed class SignalRBffNotifierTests
{
    [Fact]
    public async Task Push_To_Patient_Targets_The_Patient_Group_Async()
    {
        var context = new FakeHubContext();
        var notifier = new SignalRBffNotifier(context);
        var notification = new BffNotification { Type = "lab-result", Title = "New lab result", Summary = "3 observations" };

        await notifier.PushToPatientAsync("p-1", notification);

        context.Clients.GroupName.ShouldBe("patient:p-1");
        context.Clients.Proxy.Method.ShouldBe(NotificationsHub.EventName);
        context.Clients.Proxy.Args.ShouldNotBeNull();
        context.Clients.Proxy.Args![0].ShouldBe(notification);
    }

    [Fact]
    public async Task Push_To_User_Targets_The_User_Group_Async()
    {
        var context = new FakeHubContext();
        var notifier = new SignalRBffNotifier(context);

        await notifier.PushToUserAsync("user-sub-9", new BffNotification { Type = "ping", Title = "Hi" });

        context.Clients.GroupName.ShouldBe("user:user-sub-9");
    }

    [Fact]
    public async Task Push_To_Patient_Rejects_A_Blank_Patient_Id_Async()
    {
        var notifier = new SignalRBffNotifier(new FakeHubContext());

        await Should.ThrowAsync<ArgumentException>(() =>
            notifier.PushToPatientAsync("  ", new BffNotification { Type = "x", Title = "y" }));
    }

    private sealed class FakeHubContext : IHubContext<NotificationsHub>
    {
        public FakeHubClients Clients { get; } = new();
        IHubClients IHubContext<NotificationsHub>.Clients => Clients;
        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class FakeHubClients : IHubClients
    {
        public string? GroupName { get; private set; }
        public CapturingClientProxy Proxy { get; } = new();

        public IClientProxy Group(string groupName)
        {
            GroupName = groupName;
            return Proxy;
        }

        public IClientProxy All => throw new NotSupportedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy User(string userId) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class CapturingClientProxy : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[]? Args { get; private set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Method = method;
            Args = args;
            return Task.CompletedTask;
        }
    }
}

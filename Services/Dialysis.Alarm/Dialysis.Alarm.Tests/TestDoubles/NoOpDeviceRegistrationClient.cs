using Dialysis.Alarm.Application.Abstractions;

namespace Dialysis.Alarm.Tests.TestDoubles;

internal sealed class NoOpDeviceRegistrationClient : IDeviceRegistrationClient
{
    public Task EnsureRegisteredAsync(string deviceEui64, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

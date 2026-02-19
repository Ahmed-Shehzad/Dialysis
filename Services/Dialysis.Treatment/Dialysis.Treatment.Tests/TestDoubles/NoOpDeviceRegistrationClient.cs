using Dialysis.Treatment.Application.Abstractions;

namespace Dialysis.Treatment.Tests.TestDoubles;

internal sealed class NoOpDeviceRegistrationClient : IDeviceRegistrationClient
{
    public Task EnsureRegisteredAsync(string deviceEui64, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

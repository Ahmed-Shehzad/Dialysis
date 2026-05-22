using Dialysis.SmartConnect.TimeSync;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.TimeSync;

/// <summary>
/// Slice J2: AddSmartConnectCore must register an <see cref="IClockSkewMonitor"/> so the
/// MLLP inbound (and any other transport that adopts the §2 probe) can resolve it from
/// the DI container without each host wiring its own monitor.
/// </summary>
public sealed class ClockSkewMonitorDependencyInjectionTests
{
    [Fact]
    public void Add_Smart_Connect_Core_Registers_In_Memory_Clock_Skew_Monitor()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectCore();

        using var sp = services.BuildServiceProvider();
        var monitor = sp.GetService<IClockSkewMonitor>();

        Assert.NotNull(monitor);
        Assert.IsType<InMemoryClockSkewMonitor>(monitor);
    }

    [Fact]
    public void Clock_Skew_Monitor_Registration_Is_Singleton()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectCore();

        using var sp = services.BuildServiceProvider();
        var first = sp.GetRequiredService<IClockSkewMonitor>();
        var second = sp.GetRequiredService<IClockSkewMonitor>();

        Assert.Same(first, second);
    }
}

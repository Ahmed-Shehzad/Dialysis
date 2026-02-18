using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Tests;

public sealed class ReceiveEndpointFaultSettingsResolverTests
{
    private sealed class TestConfiguration : IReceiveEndpointConfigurationWithFaults
    {
        public TestConfiguration(ReceiveEndpointFaultSettings? faultSettings, IReadOnlyDictionary<string, object?> settings)
        {
            FaultSettings = faultSettings;
            Settings = settings;
        }

        public Uri InputAddress { get; } = new("memory://input");
        public Func<IReceiveContext, Task> Handler { get; } = _ => Task.CompletedTask;
        public IReadOnlyDictionary<string, object?> Settings { get; }
        public ReceiveEndpointFaultSettings? FaultSettings { get; }
    }

    [Fact]
    public void Resolve_Prefers_FaultSettings_Property()
    {
        var expected = new ReceiveEndpointFaultSettings();
        var settings = new Dictionary<string, object?>
        {
            [ReceiveEndpointSettingsKeys.FaultSettings] = new ReceiveEndpointFaultSettings()
        };
        var configuration = new TestConfiguration(expected, settings);

        ReceiveEndpointFaultSettings? resolved = ReceiveEndpointFaultSettingsResolver.Resolve(configuration);

        Assert.Same(expected, resolved);
    }

    [Fact]
    public void Resolve_Uses_Settings_Dictionary_When_No_Property()
    {
        var expected = new ReceiveEndpointFaultSettings();
        var settings = new Dictionary<string, object?>
        {
            [ReceiveEndpointSettingsKeys.FaultSettings] = expected
        };
        var configuration = new TestConfiguration(null, settings);

        ReceiveEndpointFaultSettings? resolved = ReceiveEndpointFaultSettingsResolver.Resolve(configuration);

        Assert.Same(expected, resolved);
    }
}

using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

public static class ReceiveEndpointFaultSettingsResolver
{
    public static ReceiveEndpointFaultSettings? Resolve(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration is IReceiveEndpointConfigurationWithFaults withFaults &&
            withFaults.FaultSettings is not null) return withFaults.FaultSettings;

        return configuration.Settings.TryGetValue(ReceiveEndpointSettingsKeys.FaultSettings, out object? value) &&
            value is ReceiveEndpointFaultSettings faultSettings
            ? faultSettings
            : null;
    }
}

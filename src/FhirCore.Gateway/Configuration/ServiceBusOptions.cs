namespace FhirCore.Gateway.Configuration;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}

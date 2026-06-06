namespace Dialysis.DataSimulator;

/// <summary>Binds the <c>DataSimulator</c> configuration section.</summary>
public sealed class DataSimulatorOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "DataSimulator";

    /// <summary>Master switch; when false the worker idles.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Seconds between generation ticks.</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>Patient journeys generated per tick.</summary>
    public int PatientsPerTick { get; set; } = 1;

    /// <summary>Base deterministic seed; combined with a per-patient counter.</summary>
    public int Seed { get; set; } = 1;

    /// <summary>Client-credentials auth settings.</summary>
    public AuthOptions Auth { get; set; } = new();

    /// <summary>Per-module base addresses.</summary>
    public ModuleAddressOptions Modules { get; set; } = new();
}

/// <summary>Keycloak client-credentials settings for the service-account bearer.</summary>
public sealed class AuthOptions
{
    /// <summary>Realm authority (token-endpoint base), e.g. <c>http://localhost:8081/realms/dialysis</c>.</summary>
    public string? Authority { get; set; }

    /// <summary>Relative token path under the authority.</summary>
    public string TokenPath { get; set; } = "/protocol/openid-connect/token";

    /// <summary>Client id.</summary>
    public string ClientId { get; set; } = "dialysis-data-simulator";

    /// <summary>Client secret.</summary>
    public string? ClientSecret { get; set; }
}

/// <summary>Per-module API base addresses (default to the compose host ports).</summary>
public sealed class ModuleAddressOptions
{
    /// <summary>HIS API base address.</summary>
    public string His { get; set; } = "http://localhost:5288";

    /// <summary>EHR API base address.</summary>
    public string Ehr { get; set; } = "http://localhost:5289";

    /// <summary>PDMS API base address.</summary>
    public string Pdms { get; set; } = "http://localhost:5290";

    /// <summary>SmartConnect API base address.</summary>
    public string SmartConnect { get; set; } = "http://localhost:5291";

    /// <summary>HIE API base address.</summary>
    public string Hie { get; set; } = "http://localhost:5292";

    /// <summary>Lab API base address.</summary>
    public string Lab { get; set; } = "http://localhost:5293";
}

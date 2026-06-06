namespace Dialysis.Simulation.Drivers;

/// <summary>Which driver implementation the engine uses.</summary>
public enum SimulationDriverMode
{
    /// <summary>Deterministic in-process stubs (default; no infrastructure).</summary>
    InMemory = 0,

    /// <summary>Real module REST APIs + integration events (requires the Aspire stack).</summary>
    Http = 1,
}

/// <summary>
/// Binds <c>Simulation:Drivers</c>. In <see cref="SimulationDriverMode.Http"/> mode the engine calls the
/// real module APIs (resolved via Aspire service discovery from the base addresses) with a
/// client-credentials bearer token minted from the configured Keycloak client.
/// </summary>
public sealed class SimulationDriverOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Simulation:Drivers";

    /// <summary>Driver implementation to register.</summary>
    public SimulationDriverMode Mode { get; set; } = SimulationDriverMode.InMemory;

    /// <summary>Keycloak realm authority (token endpoint base).</summary>
    public string? Authority { get; set; }

    /// <summary>Relative token endpoint path under the authority.</summary>
    public string TokenPath { get; set; } = "/protocol/openid-connect/token";

    /// <summary>Client-credentials client id.</summary>
    public string ClientId { get; set; } = "dialysis-simulation";

    /// <summary>Client-credentials client secret.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>EHR API base address (service-discovery name under Aspire).</summary>
    public string EhrBaseAddress { get; set; } = "http://ehr-api";

    /// <summary>HIS API base address.</summary>
    public string HisBaseAddress { get; set; } = "http://his-api";

    /// <summary>Lab API base address.</summary>
    public string LabBaseAddress { get; set; } = "http://lab-api";

    /// <summary>HIE API base address.</summary>
    public string HieBaseAddress { get; set; } = "http://hie-api";
}

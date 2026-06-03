namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Per-module bus configuration. Bound from <c>&lt;Module&gt;:DurableCommandBus</c> in
/// configuration; defaults are safe enough that most modules need only set the slug.
/// </summary>
public sealed class DurableCommandBusOptions
{
    /// <summary>Required. Owner module slug, e.g. <c>"pdms"</c>. Used for the status endpoint prefix and the ledger schema name.</summary>
    public string ModuleSlug { get; set; } = "";

    /// <summary>
    /// HTTP prefix the status endpoint mounts at. Defaults to <c>/api/v1.0/command-status</c>
    /// — every opt-in slice forms its full poll URL by appending <c>/{correlationId}</c>.
    /// </summary>
    public string StatusEndpointPrefix { get; set; } = "/api/v1.0/command-status";
}

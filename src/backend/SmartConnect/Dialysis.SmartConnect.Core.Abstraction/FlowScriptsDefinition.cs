namespace Dialysis.SmartConnect;

/// <summary>
/// Channel-level lifecycle scripts (JavaScript) executed at specific points in the message pipeline.
/// </summary>
public sealed class FlowScriptsDefinition
{
    /// <summary>Runs before route filters; can mutate or reject the message.</summary>
    public string? PreProcessorScript { get; set; }

    /// <summary>Runs after all outbound routes complete (logging, ACK generation).</summary>
    public string? PostProcessorScript { get; set; }

    /// <summary>Runs when the flow is deployed/started.</summary>
    public string? DeployScript { get; set; }

    /// <summary>Runs when the flow is undeployed/stopped.</summary>
    public string? UndeployScript { get; set; }
}

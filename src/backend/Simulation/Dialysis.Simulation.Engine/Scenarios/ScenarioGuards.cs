namespace Dialysis.Simulation.Engine.Scenarios;

/// <summary>Guards that turn a missing prerequisite id into a clear step failure.</summary>
internal static class ScenarioGuards
{
    public static Guid RequireId(Guid? value, string name) =>
        value ?? throw new InvalidOperationException($"Scenario step requires '{name}' but no prior step produced it.");

    public static string RequireValue(string? value, string name) =>
        value ?? throw new InvalidOperationException($"Scenario step requires '{name}' but no prior step produced it.");
}

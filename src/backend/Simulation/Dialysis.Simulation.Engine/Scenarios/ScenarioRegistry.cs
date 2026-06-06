namespace Dialysis.Simulation.Engine.Scenarios;

/// <summary>Resolves registered scenarios by id.</summary>
public interface IScenarioRegistry
{
    /// <summary>Every registered scenario.</summary>
    IReadOnlyList<IScenario> All { get; }

    /// <summary>Finds a scenario by id, or <c>null</c>.</summary>
    IScenario? Find(string scenarioId);
}

/// <summary>Registry backed by the <see cref="IScenario"/> implementations registered in DI.</summary>
public sealed class ScenarioRegistry : IScenarioRegistry
{
    private readonly Dictionary<string, IScenario> _byId;

    /// <summary>Creates the registry from the registered scenarios.</summary>
    public ScenarioRegistry(IEnumerable<IScenario> scenarios)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        _byId = scenarios.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        All = [.. _byId.Values];
    }

    /// <inheritdoc />
    public IReadOnlyList<IScenario> All { get; }

    /// <inheritdoc />
    public IScenario? Find(string scenarioId)
    {
        ArgumentNullException.ThrowIfNull(scenarioId);
        return _byId.GetValueOrDefault(scenarioId);
    }
}

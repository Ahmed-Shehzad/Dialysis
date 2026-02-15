namespace Dialysis.Analytics.Features.Cohorts;

/// <summary>Criterion for encounter count in period.</summary>
public sealed record EncounterCountCriterion
{
    public int MinCount { get; init; } = 1;
    public int WithinDays { get; init; } = 30;
}

/// <summary>Criterion for alert count in period.</summary>
public sealed record AlertCountCriterion
{
    public int MinCount { get; init; } = 1;
    public int WithinDays { get; init; } = 90;
}

/// <summary>Criterion for observation value (e.g. systolic BP &lt; 100).</summary>
public sealed record ObservationValueCriterion
{
    public required string Code { get; init; }
    public required string Comparator { get; init; }
    public required double Value { get; init; }
}

/// <summary>Cohort definition by criteria.</summary>
public sealed record CohortCriteria
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }

    /// <summary>Include patients with at least this many encounters in the period.</summary>
    public EncounterCountCriterion? EncounterCount { get; init; }

    /// <summary>Include patients with at least this many alerts in the period.</summary>
    public AlertCountCriterion? AlertCount { get; init; }

    /// <summary>Include patients/encounters with observation matching value filter (e.g. systolic &lt; 100).</summary>
    public ObservationValueCriterion? ObservationValue { get; init; }
}

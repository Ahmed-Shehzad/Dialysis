namespace Dialysis.Simulation.Engine.Generation;

/// <summary>A deterministically generated patient identity for a session.</summary>
public sealed record GeneratedJourney(
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    DateOnly DateOfBirth,
    string SexAtBirthCode);

/// <summary>
/// Produces the deterministic patient journey for a session. The same
/// <c>(scenarioId, tenantId, seed)</c> triple must always yield the same journey — no wall-clock time,
/// no <see cref="Guid.NewGuid"/>, no unseeded randomness.
/// </summary>
public interface IJourneyGenerator
{
    /// <summary>Generates the journey for the given scenario/tenant/seed.</summary>
    GeneratedJourney Generate(string scenarioId, string tenantId, long seed);
}

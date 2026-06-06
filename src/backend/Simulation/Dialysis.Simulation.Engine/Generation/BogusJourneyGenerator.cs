using Bogus;
using Bogus.DataSets;

namespace Dialysis.Simulation.Engine.Generation;

/// <summary>
/// Bogus-backed deterministic generator. Seeds a per-call <see cref="Randomizer"/> from a stable hash of
/// the scenario/tenant/seed, and draws every field (including the date of birth, against a fixed
/// reference date) from it — so output never depends on <c>DateTime.Now</c> or the global Bogus seed.
/// </summary>
public sealed class BogusJourneyGenerator : IJourneyGenerator
{
    // Fixed reference date so PastDateOnly is reproducible across runs/hosts.
    private static readonly DateOnly _referenceDate = new(2025, 1, 1);

    /// <inheritdoc />
    public GeneratedJourney Generate(string scenarioId, string tenantId, long seed)
    {
        ArgumentNullException.ThrowIfNull(scenarioId);
        ArgumentNullException.ThrowIfNull(tenantId);

        var derived = StableSeed(scenarioId, tenantId, seed);
        var faker = new Faker("en") { Random = new Randomizer(derived) };

        var gender = faker.Random.Bool() ? Name.Gender.Male : Name.Gender.Female;
        var family = faker.Name.LastName(gender);
        var given = faker.Name.FirstName(gender);
        var dob = faker.Date.PastDateOnly(85, _referenceDate.AddYears(-18));
        var sexCode = gender == Name.Gender.Male ? "M" : "F";
        var mrn = "SIM-" + faker.Random.ReplaceNumbers("########");

        return new GeneratedJourney(mrn, family, given, dob, sexCode);
    }

    private static int StableSeed(string scenarioId, string tenantId, long seed)
    {
        // FNV-1a 32-bit over a stable composite key (String.GetHashCode is randomized per process).
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var key = $"{scenarioId}|{tenantId}|{seed}";
        var hash = offset;
        foreach (var c in key)
        {
            hash ^= c;
            hash *= prime;
        }
        return unchecked((int)hash);
    }
}

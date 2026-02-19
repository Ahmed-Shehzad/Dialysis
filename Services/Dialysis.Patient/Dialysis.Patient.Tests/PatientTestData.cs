using Bogus;

using Dialysis.Patient.Application.Domain.ValueObjects;

namespace Dialysis.Patient.Tests;

/// <summary>
/// Bogus-generated test data for Patient tests. Uses seed for deterministic results.
/// </summary>
public static class PatientTestData
{
    static PatientTestData() => Randomizer.Seed = new Random(42);

    private static readonly Faker Faker = new();

    public static string Mrn() => Faker.Random.AlphaNumeric(8).ToUpperInvariant();

    public static Dialysis.Patient.Application.Domain.ValueObjects.Person Person() =>
        new Dialysis.Patient.Application.Domain.ValueObjects.Person(Faker.Name.FirstName(), Faker.Name.LastName());

    public static DateOnly DateOfBirth() => DateOnly.FromDateTime(Faker.Date.PastOffset(60).Date);

    public static Gender Gender() => Faker.PickRandom(Dialysis.Patient.Application.Domain.ValueObjects.Gender.Male, Dialysis.Patient.Application.Domain.ValueObjects.Gender.Female);

    /// <summary>Builds QBP^Q22 message with given MRN.</summary>
    public static string QbpQ22ByMrn(string mrn) =>
        $"""
        MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG001|P|2.6
        QPD|IHE PDQ Query^IHE PDQ Query^IHE|Q001|@PID.3^{mrn}^^^^MR
        RCP|I||RD
        """;
}

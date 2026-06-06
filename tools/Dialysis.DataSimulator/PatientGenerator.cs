using Bogus;
using Bogus.DataSets;

namespace Dialysis.DataSimulator;

/// <summary>A deterministically generated patient + the visit shape to drive for them.</summary>
public sealed record GeneratedPatient(
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    DateOnly DateOfBirth,
    string SexAtBirthCode,
    Guid ProviderId,
    bool Inpatient,
    string WardCode);

/// <summary>Produces a coherent patient from a deterministic seed (no wall-clock, no unseeded randomness).</summary>
public sealed class PatientGenerator
{
    private static readonly DateOnly _dobReference = new(2007, 1, 1);
    private static readonly string[] _wards = ["MED", "ICU", "NEPH"];

    /// <summary>Generates the patient for a given absolute sequence number.</summary>
    public GeneratedPatient Generate(int seed, long sequence)
    {
        var derived = unchecked((int)((seed * 2654435761L) + sequence));
        var faker = new Faker("en") { Random = new Randomizer(derived) };

        var gender = faker.Random.Bool() ? Name.Gender.Male : Name.Gender.Female;
        var family = faker.Name.LastName(gender);
        var given = faker.Name.FirstName(gender);
        var dob = faker.Date.PastDateOnly(85, _dobReference);
        var sex = gender == Name.Gender.Male ? "M" : "F";
        var mrn = "SIM-" + faker.Random.ReplaceNumbers("########");
        var providerId = DeterministicGuid($"provider:{faker.Random.Int(1, 8)}");
        var inpatient = faker.Random.Bool(0.35f);
        var ward = faker.PickRandom(_wards);

        return new GeneratedPatient(mrn, family, given, dob, sex, providerId, inpatient, ward);
    }

    private static Guid DeterministicGuid(string key)
    {
        var bytes = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        var guid = new byte[16];
        Array.Copy(bytes, guid, 16);
        guid[6] = (byte)((guid[6] & 0x0F) | 0x50);
        guid[8] = (byte)((guid[8] & 0x3F) | 0x80);
        return new Guid(guid);
    }
}

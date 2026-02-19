using Bogus;

namespace Dialysis.Device.Tests;

/// <summary>
/// Bogus-generated test data for Device tests. Uses seed for deterministic results.
/// </summary>
public static class DeviceTestData
{
    static DeviceTestData() => Randomizer.Seed = new Random(42);

    private static readonly Faker Faker = new();

    public static string DeviceEui64() => $"{Faker.Random.AlphaNumeric(8).ToUpperInvariant()}^EUI64^EUI-64";

    public static string DeviceId() => Faker.Random.AlphaNumeric(6).ToUpperInvariant();

    public static string Manufacturer() => Faker.Company.CompanyName();

    public static string Model() => $"HD-{Faker.Random.Number(1000, 9999)}";

    public static string SerialNumber() => $"SN{Faker.Random.AlphaNumeric(6).ToUpperInvariant()}";
}

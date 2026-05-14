namespace Dialysis.HIE.OpenEhr.Domain;

/// <summary>
/// A versioned openEHR-shaped composition. v1 stores the archetype id, version, and an opaque JSON
/// payload; the schema deliberately matches EHRbase's row shape so a future swap is a data migration.
/// </summary>
public sealed class Composition
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string ArchetypeId { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public string Composer { get; private set; } = string.Empty;
    public DateTime CommittedAtUtc { get; private set; }
    public string Payload { get; private set; } = string.Empty;

    private Composition() { }

    public Composition(Guid patientId, string archetypeId, int version, string composer, DateTime committedAtUtc, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(composer);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        Id = Guid.NewGuid();
        PatientId = patientId;
        ArchetypeId = archetypeId;
        Version = version;
        Composer = composer;
        CommittedAtUtc = committedAtUtc;
        Payload = payload;
    }
}

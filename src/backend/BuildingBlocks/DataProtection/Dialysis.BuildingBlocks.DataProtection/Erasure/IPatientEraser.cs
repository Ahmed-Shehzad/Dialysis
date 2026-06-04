namespace Dialysis.BuildingBlocks.DataProtection.Erasure;

/// <summary>
/// Per-module participation hook for the GDPR Art. 17 erasure pipeline.
///
/// Mirrors <c>IModuleDataExtractor</c> (Art. 15 / 20 export): every module that holds
/// patient-bound state ships one of these. The aggregator service walks every registered
/// eraser in turn when an operator approves an erasure request and stitches the per-module
/// results into a single audit row.
///
/// Implementations must be idempotent: re-running an erasure on a patient that has nothing
/// left to erase returns a zero count rather than throwing. Implementations should also
/// honour any legal-hold check the caller surfaced through configuration — for clinical
/// records the BDSG §10 + Berufsordnung minimum retention applies.
/// </summary>
public interface IPatientEraser
{
    /// <summary>
    /// Stable module identifier — typically the module slug (<c>"hie"</c>, <c>"pdms"</c>,
    /// <c>"ehr"</c>, <c>"his"</c>, …). Used to key the per-module entry in the composite
    /// erasure audit row.
    /// </summary>
    string ModuleSlug { get; }

    /// <summary>
    /// Erases every aggregate this module holds for <paramref name="patientId"/>. Returns
    /// the count + a per-category breakdown the audit row records so a regulator can verify
    /// the erasure end-to-end without replaying the operation.
    /// </summary>
    Task<PatientErasureResult> EraseAsync(
        Guid patientId, string approvedBy, CancellationToken cancellationToken);
}

/// <summary>
/// Per-module result of an erasure pass. <see cref="ByCategory"/> lets the audit row capture
/// what kind of aggregate was erased — e.g. for HIE Documents that's
/// <c>{ "PdmsReporting": 4, "HieInbound": 2, "AdminUpload": 1 }</c>.
/// </summary>
public sealed record PatientErasureResult
{
    /// <summary>
    /// Per-module result of an erasure pass. <see cref="ByCategory"/> lets the audit row capture
    /// what kind of aggregate was erased — e.g. for HIE Documents that's
    /// <c>{ "PdmsReporting": 4, "HieInbound": 2, "AdminUpload": 1 }</c>.
    /// </summary>
    public PatientErasureResult(int RecordsErased,
        IReadOnlyDictionary<string, int> ByCategory)
    {
        this.RecordsErased = RecordsErased;
        this.ByCategory = ByCategory;
    }
    public int RecordsErased { get; init; }
    public IReadOnlyDictionary<string, int> ByCategory { get; init; }
    public void Deconstruct(out int RecordsErased, out IReadOnlyDictionary<string, int> ByCategory)
    {
        RecordsErased = this.RecordsErased;
        ByCategory = this.ByCategory;
    }
}

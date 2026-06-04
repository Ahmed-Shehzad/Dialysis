namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Read-model snapshot the generators consume. Assembled by the consumer once per session;
/// keeping the context flat means the generators don't need to fan out queries themselves,
/// which is what makes them unit-testable in isolation.
///
/// Personally-identifying fields here are subject to the platform-wide retention policy
/// (clinical records 10 years per Berufsordnung §10) and encryption-at-rest. PHI minimisation
/// inside the generated PDFs is the responsibility of the operator-authored template.
/// </summary>
public sealed record SessionReportContext
{
    /// <summary>
    /// Read-model snapshot the generators consume. Assembled by the consumer once per session;
    /// keeping the context flat means the generators don't need to fan out queries themselves,
    /// which is what makes them unit-testable in isolation.
    ///
    /// Personally-identifying fields here are subject to the platform-wide retention policy
    /// (clinical records 10 years per Berufsordnung §10) and encryption-at-rest. PHI minimisation
    /// inside the generated PDFs is the responsibility of the operator-authored template.
    /// </summary>
    public SessionReportContext(Guid SessionId,
        Guid PatientId,
        string PatientDisplayName,
        string MedicalRecordNumber,
        string ChairLabel,
        string Modality,
        DateTime StartedAtUtc,
        DateTime CompletedAtUtc,
        int DurationMinutes,
        IReadOnlyList<VitalsSnapshot> Vitals,
        IReadOnlyList<MarEntrySnapshot> Medications,
        IReadOnlyList<AlarmSnapshot> Alarms,
        IReadOnlyList<string>? DrugAllergies = null,
        string? PendingFollowUp = null,
        string? PreferredLanguageCode = null)
    {
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.PatientDisplayName = PatientDisplayName;
        this.MedicalRecordNumber = MedicalRecordNumber;
        this.ChairLabel = ChairLabel;
        this.Modality = Modality;
        this.StartedAtUtc = StartedAtUtc;
        this.CompletedAtUtc = CompletedAtUtc;
        this.DurationMinutes = DurationMinutes;
        this.Vitals = Vitals;
        this.Medications = Medications;
        this.Alarms = Alarms;
        this.DrugAllergies = DrugAllergies;
        this.PendingFollowUp = PendingFollowUp;
        this.PreferredLanguageCode = PreferredLanguageCode;
    }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientDisplayName { get; init; }
    public string MedicalRecordNumber { get; init; }
    public string ChairLabel { get; init; }
    public string Modality { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public IReadOnlyList<VitalsSnapshot> Vitals { get; init; }
    public IReadOnlyList<MarEntrySnapshot> Medications { get; init; }
    public IReadOnlyList<AlarmSnapshot> Alarms { get; init; }
    public IReadOnlyList<string>? DrugAllergies { get; init; }
    public string? PendingFollowUp { get; init; }
    public string? PreferredLanguageCode { get; init; }
    public void Deconstruct(out Guid SessionId, out Guid PatientId, out string PatientDisplayName, out string MedicalRecordNumber, out string ChairLabel, out string Modality, out DateTime StartedAtUtc, out DateTime CompletedAtUtc, out int DurationMinutes, out IReadOnlyList<VitalsSnapshot> Vitals, out IReadOnlyList<MarEntrySnapshot> Medications, out IReadOnlyList<AlarmSnapshot> Alarms, out IReadOnlyList<string>? DrugAllergies, out string? PendingFollowUp, out string? PreferredLanguageCode)
    {
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        PatientDisplayName = this.PatientDisplayName;
        MedicalRecordNumber = this.MedicalRecordNumber;
        ChairLabel = this.ChairLabel;
        Modality = this.Modality;
        StartedAtUtc = this.StartedAtUtc;
        CompletedAtUtc = this.CompletedAtUtc;
        DurationMinutes = this.DurationMinutes;
        Vitals = this.Vitals;
        Medications = this.Medications;
        Alarms = this.Alarms;
        DrugAllergies = this.DrugAllergies;
        PendingFollowUp = this.PendingFollowUp;
        PreferredLanguageCode = this.PreferredLanguageCode;
    }
}

public sealed record VitalsSnapshot
{
    public VitalsSnapshot(DateTime CapturedAtUtc,
        decimal? BloodPressureSystolic,
        decimal? BloodPressureDiastolic,
        decimal? HeartRate,
        decimal? ArterialPressure,
        decimal? VenousPressure)
    {
        this.CapturedAtUtc = CapturedAtUtc;
        this.BloodPressureSystolic = BloodPressureSystolic;
        this.BloodPressureDiastolic = BloodPressureDiastolic;
        this.HeartRate = HeartRate;
        this.ArterialPressure = ArterialPressure;
        this.VenousPressure = VenousPressure;
    }
    public DateTime CapturedAtUtc { get; init; }
    public decimal? BloodPressureSystolic { get; init; }
    public decimal? BloodPressureDiastolic { get; init; }
    public decimal? HeartRate { get; init; }
    public decimal? ArterialPressure { get; init; }
    public decimal? VenousPressure { get; init; }
    public void Deconstruct(out DateTime CapturedAtUtc, out decimal? BloodPressureSystolic, out decimal? BloodPressureDiastolic, out decimal? HeartRate, out decimal? ArterialPressure, out decimal? VenousPressure)
    {
        CapturedAtUtc = this.CapturedAtUtc;
        BloodPressureSystolic = this.BloodPressureSystolic;
        BloodPressureDiastolic = this.BloodPressureDiastolic;
        HeartRate = this.HeartRate;
        ArterialPressure = this.ArterialPressure;
        VenousPressure = this.VenousPressure;
    }
}

public sealed record MarEntrySnapshot
{
    public MarEntrySnapshot(string MedicationDisplay,
        decimal DoseQuantity,
        string DoseUnit,
        string Route,
        DateTime AdministeredAtUtc,
        bool WasAdministered,
        string? DeclineReason)
    {
        this.MedicationDisplay = MedicationDisplay;
        this.DoseQuantity = DoseQuantity;
        this.DoseUnit = DoseUnit;
        this.Route = Route;
        this.AdministeredAtUtc = AdministeredAtUtc;
        this.WasAdministered = WasAdministered;
        this.DeclineReason = DeclineReason;
    }
    public string MedicationDisplay { get; init; }
    public decimal DoseQuantity { get; init; }
    public string DoseUnit { get; init; }
    public string Route { get; init; }
    public DateTime AdministeredAtUtc { get; init; }
    public bool WasAdministered { get; init; }
    public string? DeclineReason { get; init; }
    public void Deconstruct(out string MedicationDisplay, out decimal DoseQuantity, out string DoseUnit, out string Route, out DateTime AdministeredAtUtc, out bool WasAdministered, out string? DeclineReason)
    {
        MedicationDisplay = this.MedicationDisplay;
        DoseQuantity = this.DoseQuantity;
        DoseUnit = this.DoseUnit;
        Route = this.Route;
        AdministeredAtUtc = this.AdministeredAtUtc;
        WasAdministered = this.WasAdministered;
        DeclineReason = this.DeclineReason;
    }
}

public sealed record AlarmSnapshot
{
    public AlarmSnapshot(string AlarmCode,
        string AlarmText,
        string Severity,
        DateTime RaisedAtUtc,
        bool Acknowledged)
    {
        this.AlarmCode = AlarmCode;
        this.AlarmText = AlarmText;
        this.Severity = Severity;
        this.RaisedAtUtc = RaisedAtUtc;
        this.Acknowledged = Acknowledged;
    }
    public string AlarmCode { get; init; }
    public string AlarmText { get; init; }
    public string Severity { get; init; }
    public DateTime RaisedAtUtc { get; init; }
    public bool Acknowledged { get; init; }
    public void Deconstruct(out string AlarmCode, out string AlarmText, out string Severity, out DateTime RaisedAtUtc, out bool Acknowledged)
    {
        AlarmCode = this.AlarmCode;
        AlarmText = this.AlarmText;
        Severity = this.Severity;
        RaisedAtUtc = this.RaisedAtUtc;
        Acknowledged = this.Acknowledged;
    }
}

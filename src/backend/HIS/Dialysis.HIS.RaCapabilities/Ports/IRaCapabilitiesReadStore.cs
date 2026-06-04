namespace Dialysis.HIS.RaCapabilities.Ports;

public interface IRaCapabilitiesReadStore
{
    Task<IReadOnlyList<RaOrgCommunicationRow>> ListOrganizationalCommunicationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaQualityWorkflowTaskRow>> ListQualityWorkflowTasksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaFinancialErpLinkRow>> ListFinancialErpLinksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaWaitlistEntryRow>> ListWaitlistEntriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaEhrDocumentExchangeRow>> ListEhrDocumentExchangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaPatientAlertRow>> ListPatientAlertsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaMedicationDispensingRow>> ListMedicationDispensingRecordsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaClinicalDecisionSupportRow>> ListClinicalDecisionSupportEvaluationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaAnalyticsExportJobRow>> ListAnalyticsExportJobsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaFullTextSearchEntryRow>> ListFullTextSearchEntriesAsync(
        string? searchTextContains,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaSecurityMechanismRow>> ListSecurityMechanismHardeningsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaSpecialistEncounterRow>> ListSpecialistEncountersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaResearchEducationActivityRow>> ListResearchEducationActivitiesAsync(CancellationToken cancellationToken = default);
}

public sealed record RaOrgCommunicationRow
{
    public RaOrgCommunicationRow(Guid Id, string ThreadCode, string Subject, string Body, DateTime SentAtUtc)
    {
        this.Id = Id;
        this.ThreadCode = ThreadCode;
        this.Subject = Subject;
        this.Body = Body;
        this.SentAtUtc = SentAtUtc;
    }
    public Guid Id { get; init; }
    public string ThreadCode { get; init; }
    public string Subject { get; init; }
    public string Body { get; init; }
    public DateTime SentAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out string ThreadCode, out string Subject, out string Body, out DateTime SentAtUtc)
    {
        Id = this.Id;
        ThreadCode = this.ThreadCode;
        Subject = this.Subject;
        Body = this.Body;
        SentAtUtc = this.SentAtUtc;
    }
}

public sealed record RaQualityWorkflowTaskRow
{
    public RaQualityWorkflowTaskRow(Guid Id,
        string TaskCode,
        string Title,
        string StatusCode,
        DateTime OpenedAtUtc,
        DateTime? ClosedAtUtc)
    {
        this.Id = Id;
        this.TaskCode = TaskCode;
        this.Title = Title;
        this.StatusCode = StatusCode;
        this.OpenedAtUtc = OpenedAtUtc;
        this.ClosedAtUtc = ClosedAtUtc;
    }
    public Guid Id { get; init; }
    public string TaskCode { get; init; }
    public string Title { get; init; }
    public string StatusCode { get; init; }
    public DateTime OpenedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out string TaskCode, out string Title, out string StatusCode, out DateTime OpenedAtUtc, out DateTime? ClosedAtUtc)
    {
        Id = this.Id;
        TaskCode = this.TaskCode;
        Title = this.Title;
        StatusCode = this.StatusCode;
        OpenedAtUtc = this.OpenedAtUtc;
        ClosedAtUtc = this.ClosedAtUtc;
    }
}

public sealed record RaFinancialErpLinkRow
{
    public RaFinancialErpLinkRow(Guid Id, string SystemCode, DateTime? LastHandshakeAtUtc, string StatusCode)
    {
        this.Id = Id;
        this.SystemCode = SystemCode;
        this.LastHandshakeAtUtc = LastHandshakeAtUtc;
        this.StatusCode = StatusCode;
    }
    public Guid Id { get; init; }
    public string SystemCode { get; init; }
    public DateTime? LastHandshakeAtUtc { get; init; }
    public string StatusCode { get; init; }
    public void Deconstruct(out Guid Id, out string SystemCode, out DateTime? LastHandshakeAtUtc, out string StatusCode)
    {
        Id = this.Id;
        SystemCode = this.SystemCode;
        LastHandshakeAtUtc = this.LastHandshakeAtUtc;
        StatusCode = this.StatusCode;
    }
}

public sealed record RaWaitlistEntryRow
{
    public RaWaitlistEntryRow(Guid Id,
        Guid PatientId,
        string ResourceKindCode,
        string Notes,
        DateTime RequestedNotBeforeUtc,
        DateTime EnqueuedAtUtc)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.ResourceKindCode = ResourceKindCode;
        this.Notes = Notes;
        this.RequestedNotBeforeUtc = RequestedNotBeforeUtc;
        this.EnqueuedAtUtc = EnqueuedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string ResourceKindCode { get; init; }
    public string Notes { get; init; }
    public DateTime RequestedNotBeforeUtc { get; init; }
    public DateTime EnqueuedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string ResourceKindCode, out string Notes, out DateTime RequestedNotBeforeUtc, out DateTime EnqueuedAtUtc)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        ResourceKindCode = this.ResourceKindCode;
        Notes = this.Notes;
        RequestedNotBeforeUtc = this.RequestedNotBeforeUtc;
        EnqueuedAtUtc = this.EnqueuedAtUtc;
    }
}

public sealed record RaEhrDocumentExchangeRow
{
    public RaEhrDocumentExchangeRow(Guid Id,
        Guid PatientId,
        string DocumentTypeCode,
        string ExternalSystemCode,
        string ExternalUri,
        DateTime ExchangedAtUtc)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.DocumentTypeCode = DocumentTypeCode;
        this.ExternalSystemCode = ExternalSystemCode;
        this.ExternalUri = ExternalUri;
        this.ExchangedAtUtc = ExchangedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string DocumentTypeCode { get; init; }
    public string ExternalSystemCode { get; init; }
    public string ExternalUri { get; init; }
    public DateTime ExchangedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string DocumentTypeCode, out string ExternalSystemCode, out string ExternalUri, out DateTime ExchangedAtUtc)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        DocumentTypeCode = this.DocumentTypeCode;
        ExternalSystemCode = this.ExternalSystemCode;
        ExternalUri = this.ExternalUri;
        ExchangedAtUtc = this.ExchangedAtUtc;
    }
}

public sealed record RaPatientAlertRow
{
    public RaPatientAlertRow(Guid Id,
        Guid PatientId,
        string RuleCode,
        string Severity,
        string Message,
        DateTime RaisedAtUtc,
        DateTime? ClearedAtUtc)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.RuleCode = RuleCode;
        this.Severity = Severity;
        this.Message = Message;
        this.RaisedAtUtc = RaisedAtUtc;
        this.ClearedAtUtc = ClearedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string RuleCode { get; init; }
    public string Severity { get; init; }
    public string Message { get; init; }
    public DateTime RaisedAtUtc { get; init; }
    public DateTime? ClearedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string RuleCode, out string Severity, out string Message, out DateTime RaisedAtUtc, out DateTime? ClearedAtUtc)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        RuleCode = this.RuleCode;
        Severity = this.Severity;
        Message = this.Message;
        RaisedAtUtc = this.RaisedAtUtc;
        ClearedAtUtc = this.ClearedAtUtc;
    }
}

public sealed record RaMedicationDispensingRow
{
    public RaMedicationDispensingRow(Guid Id, Guid MedicationOrderId, string BarcodeToken, DateTime DispensedAtUtc)
    {
        this.Id = Id;
        this.MedicationOrderId = MedicationOrderId;
        this.BarcodeToken = BarcodeToken;
        this.DispensedAtUtc = DispensedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid MedicationOrderId { get; init; }
    public string BarcodeToken { get; init; }
    public DateTime DispensedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out Guid MedicationOrderId, out string BarcodeToken, out DateTime DispensedAtUtc)
    {
        Id = this.Id;
        MedicationOrderId = this.MedicationOrderId;
        BarcodeToken = this.BarcodeToken;
        DispensedAtUtc = this.DispensedAtUtc;
    }
}

public sealed record RaClinicalDecisionSupportRow
{
    public RaClinicalDecisionSupportRow(Guid Id,
        Guid PatientId,
        string ChecksAppliedJson,
        bool SafeToProceed,
        DateTime EvaluatedAtUtc)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.ChecksAppliedJson = ChecksAppliedJson;
        this.SafeToProceed = SafeToProceed;
        this.EvaluatedAtUtc = EvaluatedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string ChecksAppliedJson { get; init; }
    public bool SafeToProceed { get; init; }
    public DateTime EvaluatedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string ChecksAppliedJson, out bool SafeToProceed, out DateTime EvaluatedAtUtc)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        ChecksAppliedJson = this.ChecksAppliedJson;
        SafeToProceed = this.SafeToProceed;
        EvaluatedAtUtc = this.EvaluatedAtUtc;
    }
}

public sealed record RaAnalyticsExportJobRow
{
    public RaAnalyticsExportJobRow(Guid Id, string PipelineCode, DateTime RequestedAtUtc, string StatusCode)
    {
        this.Id = Id;
        this.PipelineCode = PipelineCode;
        this.RequestedAtUtc = RequestedAtUtc;
        this.StatusCode = StatusCode;
    }
    public Guid Id { get; init; }
    public string PipelineCode { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public string StatusCode { get; init; }
    public void Deconstruct(out Guid Id, out string PipelineCode, out DateTime RequestedAtUtc, out string StatusCode)
    {
        Id = this.Id;
        PipelineCode = this.PipelineCode;
        RequestedAtUtc = this.RequestedAtUtc;
        StatusCode = this.StatusCode;
    }
}

public sealed record RaFullTextSearchEntryRow
{
    public RaFullTextSearchEntryRow(Guid Id, string CorpusCode, string ExternalId, string SearchText, DateTime IndexedAtUtc)
    {
        this.Id = Id;
        this.CorpusCode = CorpusCode;
        this.ExternalId = ExternalId;
        this.SearchText = SearchText;
        this.IndexedAtUtc = IndexedAtUtc;
    }
    public Guid Id { get; init; }
    public string CorpusCode { get; init; }
    public string ExternalId { get; init; }
    public string SearchText { get; init; }
    public DateTime IndexedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out string CorpusCode, out string ExternalId, out string SearchText, out DateTime IndexedAtUtc)
    {
        Id = this.Id;
        CorpusCode = this.CorpusCode;
        ExternalId = this.ExternalId;
        SearchText = this.SearchText;
        IndexedAtUtc = this.IndexedAtUtc;
    }
}

public sealed record RaSecurityMechanismRow
{
    public RaSecurityMechanismRow(Guid Id, string MechanismCode, string AppliedLevel, string Notes, DateTime AssessedAtUtc)
    {
        this.Id = Id;
        this.MechanismCode = MechanismCode;
        this.AppliedLevel = AppliedLevel;
        this.Notes = Notes;
        this.AssessedAtUtc = AssessedAtUtc;
    }
    public Guid Id { get; init; }
    public string MechanismCode { get; init; }
    public string AppliedLevel { get; init; }
    public string Notes { get; init; }
    public DateTime AssessedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out string MechanismCode, out string AppliedLevel, out string Notes, out DateTime AssessedAtUtc)
    {
        Id = this.Id;
        MechanismCode = this.MechanismCode;
        AppliedLevel = this.AppliedLevel;
        Notes = this.Notes;
        AssessedAtUtc = this.AssessedAtUtc;
    }
}

public sealed record RaSpecialistEncounterRow
{
    public RaSpecialistEncounterRow(Guid Id,
        Guid PatientId,
        string SpecialtyCode,
        string ExternalSystemCode,
        string Summary,
        DateTime RecordedAtUtc)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.SpecialtyCode = SpecialtyCode;
        this.ExternalSystemCode = ExternalSystemCode;
        this.Summary = Summary;
        this.RecordedAtUtc = RecordedAtUtc;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string SpecialtyCode { get; init; }
    public string ExternalSystemCode { get; init; }
    public string Summary { get; init; }
    public DateTime RecordedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string SpecialtyCode, out string ExternalSystemCode, out string Summary, out DateTime RecordedAtUtc)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        SpecialtyCode = this.SpecialtyCode;
        ExternalSystemCode = this.ExternalSystemCode;
        Summary = this.Summary;
        RecordedAtUtc = this.RecordedAtUtc;
    }
}

public sealed record RaResearchEducationActivityRow
{
    public RaResearchEducationActivityRow(Guid Id,
        string ActivityKindCode,
        string Title,
        string ExternalReference,
        DateTime RecordedAtUtc)
    {
        this.Id = Id;
        this.ActivityKindCode = ActivityKindCode;
        this.Title = Title;
        this.ExternalReference = ExternalReference;
        this.RecordedAtUtc = RecordedAtUtc;
    }
    public Guid Id { get; init; }
    public string ActivityKindCode { get; init; }
    public string Title { get; init; }
    public string ExternalReference { get; init; }
    public DateTime RecordedAtUtc { get; init; }
    public void Deconstruct(out Guid Id, out string ActivityKindCode, out string Title, out string ExternalReference, out DateTime RecordedAtUtc)
    {
        Id = this.Id;
        ActivityKindCode = this.ActivityKindCode;
        Title = this.Title;
        ExternalReference = this.ExternalReference;
        RecordedAtUtc = this.RecordedAtUtc;
    }
}

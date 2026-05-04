namespace Dialysis.HIS.RaCapabilities.Domain;

public sealed class RaOrgCommunication
{
    public Guid Id { get; set; }

    public string ThreadCode { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }
}

public sealed class RaQualityWorkflowTask
{
    public Guid Id { get; set; }

    public string TaskCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public DateTime OpenedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }
}

public sealed class RaFinancialErpLink
{
    public Guid Id { get; set; }

    public string SystemCode { get; set; } = string.Empty;

    public DateTime? LastHandshakeAtUtc { get; set; }

    public string StatusCode { get; set; } = string.Empty;
}

public sealed class RaWaitlistEntry
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string ResourceKindCode { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime RequestedNotBeforeUtc { get; set; }

    public DateTime EnqueuedAtUtc { get; set; }
}

public sealed class RaEhrDocumentExchangeRecord
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string DocumentTypeCode { get; set; } = string.Empty;

    public string ExternalSystemCode { get; set; } = string.Empty;

    public string ExternalUri { get; set; } = string.Empty;

    public DateTime ExchangedAtUtc { get; set; }
}

public sealed class RaPatientAlert
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime RaisedAtUtc { get; set; }

    public DateTime? ClearedAtUtc { get; set; }
}

public sealed class RaMedicationDispensingRecord
{
    public Guid Id { get; set; }

    public Guid MedicationOrderId { get; set; }

    public string BarcodeToken { get; set; } = string.Empty;

    public DateTime DispensedAtUtc { get; set; }
}

public sealed class RaClinicalDecisionSupportEvaluation
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string ChecksAppliedJson { get; set; } = string.Empty;

    public bool SafeToProceed { get; set; }

    public DateTime EvaluatedAtUtc { get; set; }
}

public sealed class RaAnalyticsExportJob
{
    public Guid Id { get; set; }

    public string PipelineCode { get; set; } = string.Empty;

    public DateTime RequestedAtUtc { get; set; }

    public string StatusCode { get; set; } = string.Empty;
}

public sealed class RaFullTextSearchEntry
{
    public Guid Id { get; set; }

    public string CorpusCode { get; set; } = string.Empty;

    public string ExternalId { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public DateTime IndexedAtUtc { get; set; }
}

public sealed class RaSecurityMechanismHardening
{
    public Guid Id { get; set; }

    public string MechanismCode { get; set; } = string.Empty;

    public string AppliedLevel { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime AssessedAtUtc { get; set; }
}

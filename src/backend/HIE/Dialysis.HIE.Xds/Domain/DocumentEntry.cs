namespace Dialysis.HIE.Xds.Domain;

/// <summary>IHE XDS DocumentEntry metadata (ITI-42 register / ITI-18 query).</summary>
public sealed record DocumentEntry
{
    /// <summary>IHE XDS DocumentEntry metadata (ITI-42 register / ITI-18 query).</summary>
    public DocumentEntry(string UniqueId,
        string PatientId,
        string MimeType,
        string FormatCode,
        string ClassCode,
        string TypeCode,
        string ConfidentialityCode,
        string SourceOrgId,
        DateTimeOffset CreationTime,
        string? Title,
        string? RepositoryUniqueId,
        long Size)
    {
        this.UniqueId = UniqueId;
        this.PatientId = PatientId;
        this.MimeType = MimeType;
        this.FormatCode = FormatCode;
        this.ClassCode = ClassCode;
        this.TypeCode = TypeCode;
        this.ConfidentialityCode = ConfidentialityCode;
        this.SourceOrgId = SourceOrgId;
        this.CreationTime = CreationTime;
        this.Title = Title;
        this.RepositoryUniqueId = RepositoryUniqueId;
        this.Size = Size;
    }
    public string UniqueId { get; init; }
    public string PatientId { get; init; }
    public string MimeType { get; init; }
    public string FormatCode { get; init; }
    public string ClassCode { get; init; }
    public string TypeCode { get; init; }
    public string ConfidentialityCode { get; init; }
    public string SourceOrgId { get; init; }
    public DateTimeOffset CreationTime { get; init; }
    public string? Title { get; init; }
    public string? RepositoryUniqueId { get; init; }
    public long Size { get; init; }
    public void Deconstruct(out string UniqueId, out string PatientId, out string MimeType, out string FormatCode, out string ClassCode, out string TypeCode, out string ConfidentialityCode, out string SourceOrgId, out DateTimeOffset CreationTime, out string? Title, out string? RepositoryUniqueId, out long Size)
    {
        UniqueId = this.UniqueId;
        PatientId = this.PatientId;
        MimeType = this.MimeType;
        FormatCode = this.FormatCode;
        ClassCode = this.ClassCode;
        TypeCode = this.TypeCode;
        ConfidentialityCode = this.ConfidentialityCode;
        SourceOrgId = this.SourceOrgId;
        CreationTime = this.CreationTime;
        Title = this.Title;
        RepositoryUniqueId = this.RepositoryUniqueId;
        Size = this.Size;
    }
}

public sealed record SubmissionSet
{
    public SubmissionSet(string UniqueId,
        string PatientId,
        string SourceId,
        DateTimeOffset SubmissionTime,
        IReadOnlyList<string> DocumentUniqueIds)
    {
        this.UniqueId = UniqueId;
        this.PatientId = PatientId;
        this.SourceId = SourceId;
        this.SubmissionTime = SubmissionTime;
        this.DocumentUniqueIds = DocumentUniqueIds;
    }
    public string UniqueId { get; init; }
    public string PatientId { get; init; }
    public string SourceId { get; init; }
    public DateTimeOffset SubmissionTime { get; init; }
    public IReadOnlyList<string> DocumentUniqueIds { get; init; }
    public void Deconstruct(out string UniqueId, out string PatientId, out string SourceId, out DateTimeOffset SubmissionTime, out IReadOnlyList<string> DocumentUniqueIds)
    {
        UniqueId = this.UniqueId;
        PatientId = this.PatientId;
        SourceId = this.SourceId;
        SubmissionTime = this.SubmissionTime;
        DocumentUniqueIds = this.DocumentUniqueIds;
    }
}

namespace Dialysis.HIE.Xds.Domain;

/// <summary>IHE XDS DocumentEntry metadata (ITI-42 register / ITI-18 query).</summary>
public sealed record DocumentEntry(
    string UniqueId,
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
    long Size);

public sealed record SubmissionSet(
    string UniqueId,
    string PatientId,
    string SourceId,
    DateTimeOffset SubmissionTime,
    IReadOnlyList<string> DocumentUniqueIds);

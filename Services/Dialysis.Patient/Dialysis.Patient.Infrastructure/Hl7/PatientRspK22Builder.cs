using System.Globalization;

using Dialysis.Patient.Application.Abstractions;

using DomainPatient = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Infrastructure.Hl7;

/// <summary>
/// Builds HL7 RSP^K22 patient demographics response messages (IHE ITI-21).
/// </summary>
public sealed class PatientRspK22Builder : IPatientRspK22Builder
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';

    public string BuildFromPatients(IReadOnlyList<DomainPatient> patients, QbpQ22ParseResult query)
    {
        string messageControlId = query.MessageControlId ?? Ulid.NewUlid().ToString();
        string queryTag = query.QueryTag ?? messageControlId;

        string queryName = query.QueryName ?? "IHE PDQ Query";
        var segments = new List<string>
        {
            BuildMsh(messageControlId),
            BuildMsa("AA", messageControlId),
            BuildQak(queryTag, "OK", patients.Count, queryName),
            BuildQpd(queryTag, query)
        };

        segments.AddRange(patients.Select(BuildPid));

        return string.Join("\r\n", segments) + "\r\n";
    }

    public string BuildNoDataFound(QbpQ22ParseResult query)
    {
        string messageControlId = query.MessageControlId ?? Ulid.NewUlid().ToString();
        string queryTag = query.QueryTag ?? messageControlId;

        string queryName = query.QueryName ?? "IHE PDQ Query";
        var segments = new List<string>
        {
            BuildMsh(messageControlId),
            BuildMsa("AA", messageControlId),
            BuildQak(queryTag, "NF", 0, queryName),
            BuildQpd(queryTag, query)
        };

        return string.Join("\r\n", segments) + "\r\n";
    }

    /// <summary>Builds RSP^K22 with application error (AE) or reject (AR).</summary>
    public string BuildError(QbpQ22ParseResult query, string ackCode = "AE")
    {
        string messageControlId = query.MessageControlId ?? Ulid.NewUlid().ToString();
        string queryTag = query.QueryTag ?? messageControlId;
        string queryName = query.QueryName ?? "IHE PDQ Query";
        string msaCode = ackCode is "AR" or "AE" ? ackCode : "AE";
        var segments = new List<string>
        {
            BuildMsh(messageControlId),
            BuildMsa(msaCode, messageControlId),
            BuildQak(queryTag, msaCode, 0, queryName),
            BuildQpd(queryTag, query)
        };
        return string.Join("\r\n", segments) + "\r\n";
    }

    private static string BuildMsh(string messageControlId)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"MSH{FieldSeparator}^~\\&{FieldSeparator}PDMS{FieldSeparator}FAC{FieldSeparator}MACH{FieldSeparator}FAC{FieldSeparator}{timestamp}{FieldSeparator}{FieldSeparator}RSP{ComponentSeparator}K22{ComponentSeparator}RSP_K21{FieldSeparator}{messageControlId}{FieldSeparator}P{FieldSeparator}2.6";
    }

    private static string BuildMsa(string ackCode, string messageControlId) =>
        $"MSA{FieldSeparator}{ackCode}{FieldSeparator}{messageControlId}";

    private static string BuildQak(string queryTag, string status, int matchCount, string? queryName = null)
    {
        string qak3 = queryName ?? "";
        return $"QAK{FieldSeparator}{queryTag}{FieldSeparator}{status}{FieldSeparator}{qak3}{FieldSeparator}{matchCount}";
    }

    private static string BuildQpd(string queryTag, QbpQ22ParseResult query)
    {
        string searchParam = !string.IsNullOrEmpty(query.Mrn)
            ? $"@PID.3.1{ComponentSeparator}{query.Mrn}"
            : $"@PID.5.1{ComponentSeparator}{query.LastName ?? ""}~@PID.5.2{ComponentSeparator}{query.FirstName ?? ""}";

        return $"QPD{FieldSeparator}IHE PDQ Query{FieldSeparator}{queryTag}{FieldSeparator}{searchParam}";
    }

    private static string BuildPid(DomainPatient patient)
    {
        string pid3 = BuildPid3(patient);
        string name = $"{patient.Name.LastName}{ComponentSeparator}{patient.Name.FirstName}";
        string dob = patient.DateOfBirth.HasValue
            ? patient.DateOfBirth.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : "";
        string gender = patient.Gender?.Value ?? "";

        return $"PID{FieldSeparator}{FieldSeparator}{FieldSeparator}{pid3}{FieldSeparator}{FieldSeparator}{name}{FieldSeparator}{FieldSeparator}{dob}{FieldSeparator}{gender}";
    }

    private static string BuildPid3(DomainPatient patient)
    {
        string id = patient.MedicalRecordNumber.Value;
        string type = "MR";
        return $"{id}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{type}";
    }
}

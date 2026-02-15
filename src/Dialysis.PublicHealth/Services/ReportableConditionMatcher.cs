using Dialysis.PublicHealth.Models;
using Hl7.Fhir.Model;

namespace Dialysis.PublicHealth.Services;

/// <summary>Matches FHIR Condition, Observation, or Procedure to reportable conditions from the catalog.</summary>
public sealed class ReportableConditionMatcher
{
    private readonly IReportableConditionCatalog _catalog;

    public ReportableConditionMatcher(IReportableConditionCatalog catalog)
    {
        _catalog = catalog;
    }

    /// <summary>Find reportable conditions matching the resource's codes. Optionally filter by jurisdiction.</summary>
    public async Task<IReadOnlyList<ReportableConditionMatch>> MatchAsync(Resource resource, string? jurisdiction = null, CancellationToken cancellationToken = default)
    {
        var codes = ExtractCodes(resource);
        if (codes.Count == 0) return Array.Empty<ReportableConditionMatch>();

        var conditions = await _catalog.ListAsync(jurisdiction, cancellationToken);
        var matches = new List<ReportableConditionMatch>();

        foreach (var cond in conditions.Where(c => c.IsActive))
        {
            foreach (var (code, system) in codes)
            {
                if (CodeMatches(cond.Code, code, system))
                {
                    matches.Add(new ReportableConditionMatch(cond, code, system));
                    break;
                }
            }
        }

        return matches;
    }

    private static List<(string Code, string? System)> ExtractCodes(Resource resource)
    {
        var list = new List<(string, string?)>();
        if (resource is Condition c && c.Code != null)
            AddCodings(list, c.Code);
        else if (resource is Observation o && o.Code != null)
            AddCodings(list, o.Code);
        else if (resource is Procedure p && p.Code != null)
            AddCodings(list, p.Code);
        return list;
    }

    private static void AddCodings(List<(string Code, string? System)> list, CodeableConcept concept)
    {
        foreach (var coding in concept.Coding ?? [])
        {
            if (!string.IsNullOrWhiteSpace(coding.Code))
                list.Add((coding.Code, coding.System));
        }
    }

    /// <summary>Match catalog code to resource code. Supports exact match and ICD-10 hierarchy (e.g. A41 matches A41.9).</summary>
    private static bool CodeMatches(string catalogCode, string resourceCode, string? system)
    {
        if (string.Equals(catalogCode, resourceCode, StringComparison.OrdinalIgnoreCase))
            return true;
        if (IsIcd10(system) && resourceCode.StartsWith(catalogCode, StringComparison.OrdinalIgnoreCase) &&
            (resourceCode.Length == catalogCode.Length || resourceCode[catalogCode.Length] == '.'))
            return true;
        return false;
    }

    private static bool IsIcd10(string? system)
    {
        if (string.IsNullOrEmpty(system)) return false;
        return system.Contains("icd10", StringComparison.OrdinalIgnoreCase) ||
               system.Contains("hl7.org/sid/icd-10", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ReportableConditionMatch(ReportableCondition Condition, string MatchedCode, string? CodeSystem);

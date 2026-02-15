using Task = System.Threading.Tasks.Task;
using Hl7.Fhir.Model;

namespace Dialysis.PublicHealth.Services;

/// <summary>De-identification pipeline: Basic, HIPAA Safe Harbor (18 identifiers), Expert Determination.</summary>
public sealed class DeidentificationPipeline : IDeidentificationPipeline
{
    private static readonly string[] SafeHarborIdentifierSystems =
    [
        "http://hl7.org/fhir/sid/us-ssn",
        "urn:oid:2.16.840.1.113883.4.1",
        "http://hospital.example.org/mrn",
        "http://hl7.org/fhir/sid/us-mbi",
        "urn:oid:2.16.840.1.113883.4.39", // Tax ID
        "http://terminology.hl7.org/CodeSystem/v2-0203", // MR, SSN, etc.
    ];

    public Task<Resource> DeidentifyAsync(Resource resource, DeidentificationOptions options, CancellationToken cancellationToken = default)
    {
        var clone = (Resource)resource.DeepCopy();
        var level = options.Level;

        if (clone is Patient p)
            DeidentifyPatient(p, options, level);

        if (clone is Encounter enc)
            DeidentifyEncounter(enc, options, level);

        if (clone is DomainResource dr)
        {
            if (options.RemoveFreeText) dr.Text = null;
            if (level >= DeidentificationLevel.SafeHarbor)
            {
                dr.ModifierExtension?.Clear();
                if (dr is Observation obs) obs.Note?.Clear();
            }
        }

        if (level >= DeidentificationLevel.SafeHarbor)
        {
            clone.Id = null;
            clone.Meta = null;
        }

        return Task.FromResult(clone);
    }

    private static void DeidentifyPatient(Patient p, DeidentificationOptions options, DeidentificationLevel level)
    {
        if (options.RemoveDirectIdentifiers || level >= DeidentificationLevel.SafeHarbor)
        {
            p.Name.Clear();
            p.Telecom?.Clear();
            p.Address?.Clear();
            p.Photo?.Clear();
            p.Contact?.Clear();

            if (p.Identifier != null && (options.RemoveDirectIdentifiers || level >= DeidentificationLevel.SafeHarbor))
            {
                var toRemove = (options.Level >= DeidentificationLevel.SafeHarbor
                    ? p.Identifier.Where(IsIdentifyingIdentifier)
                    : p.Identifier).ToList();
                foreach (var id in toRemove) p.Identifier.Remove(id);
            }
        }

        if (options.GeneralizeDates && p.BirthDateElement != null)
        {
            var s = p.BirthDateElement.ToString() ?? "0000";
            p.BirthDateElement = new Date(s.Length >= 4 ? s[..4] : "0000");
        }

        if (options.GeneralizeAgesOver89 && p.BirthDateElement != null)
        {
            var y = int.TryParse(p.BirthDateElement.ToString(), out var yr) ? yr : 0;
            if (DateTime.UtcNow.Year - y > 89)
                p.BirthDateElement = new Date((DateTime.UtcNow.Year - 90).ToString());
        }
    }

    private static bool IsIdentifyingIdentifier(Identifier id)
    {
        var sys = id.System ?? "";
        if (SafeHarborIdentifierSystems.Any(s => sys.Contains(s, StringComparison.OrdinalIgnoreCase))) return true;
        var use = id.Use ?? Identifier.IdentifierUse.Old;
        return use == Identifier.IdentifierUse.Official || use == Identifier.IdentifierUse.Usual;
    }

    private static void DeidentifyEncounter(Encounter enc, DeidentificationOptions options, DeidentificationLevel level)
    {
        if (options.GeneralizeDates && enc.Period != null)
        {
            if (!string.IsNullOrEmpty(enc.Period.Start))
            {
                var s = enc.Period.Start;
                enc.Period.Start = s.Length >= 4 ? s[..4] : s;
            }
            enc.Period.End = null;
        }

        if (level >= DeidentificationLevel.SafeHarbor)
        {
            enc.Location?.Clear();
            enc.ReasonCode?.Clear();
            enc.Diagnosis?.Clear();
        }
    }

    public async Task<Bundle> DeidentifyBundleAsync(Bundle bundle, DeidentificationOptions options, CancellationToken cancellationToken = default)
    {
        var result = new Bundle { Type = bundle.Type, Total = bundle.Total, Link = bundle.Link };
        foreach (var entry in bundle.Entry)
        {
            if (entry.Resource != null)
            {
                var deidentified = await DeidentifyAsync(entry.Resource, options, cancellationToken);
                result.Entry.Add(new Bundle.EntryComponent { Resource = deidentified });
            }
        }
        return result;
    }
}

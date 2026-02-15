using Dialysis.Documents.Configuration;
using GdPicture14;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Documents.Services;

/// <summary>Fills AcroForm PDF templates using Nutrient .NET SDK (GdPicture).</summary>
public sealed class NutrientPdfTemplateFiller : IPdfTemplateFiller
{
    private readonly IFhirDataResolver _fhirResolver;
    private readonly DocumentsOptions _options;
    private readonly ILogger<NutrientPdfTemplateFiller> _logger;

    public NutrientPdfTemplateFiller(
        IFhirDataResolver fhirResolver,
        IOptions<DocumentsOptions> options,
        ILogger<NutrientPdfTemplateFiller> logger)
    {
        _fhirResolver = fhirResolver;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> FillAsync(
        string templateId,
        string? patientId,
        string? encounterId,
        IReadOnlyDictionary<string, string>? mappings,
        bool includeScripts,
        CancellationToken cancellationToken = default)
    {
        var templatePath = ResolveTemplatePath(templateId);
        if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template '{templateId}' not found at {_options.TemplatePath}");
        }

        var fieldValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (mappings != null)
        {
            foreach (var kv in mappings)
                fieldValues[kv.Key] = kv.Value;
        }

        if (!string.IsNullOrEmpty(patientId))
        {
            var patientData = await _fhirResolver.GetPatientFieldValuesAsync(patientId, cancellationToken);
            foreach (var kv in patientData)
            {
                if (!fieldValues.ContainsKey(kv.Key))
                    fieldValues[kv.Key] = kv.Value;
            }
        }

        if (!string.IsNullOrEmpty(encounterId))
        {
            var encounterData = await _fhirResolver.GetEncounterFieldValuesAsync(encounterId, cancellationToken);
            foreach (var kv in encounterData)
            {
                if (!fieldValues.ContainsKey(kv.Key))
                    fieldValues[kv.Key] = kv.Value;
            }
        }

        if (includeScripts && IsCalculatorTemplate(templateId))
            PreCalculateAdequacy(fieldValues);

        using var pdf = new GdPicturePDF();
        if (pdf.LoadFromFile(templatePath, false) != GdPictureStatus.OK)
            throw new InvalidOperationException($"Failed to load PDF template: {templatePath}");

        var fieldCount = pdf.GetFormFieldsCount();
        for (var i = 0; i < fieldCount; i++)
        {
            var fieldId = pdf.GetFormFieldId(i);
            if (pdf.GetStat() != GdPictureStatus.OK) break;

            var fieldTitle = pdf.GetFormFieldTitle(fieldId);
            if (pdf.GetStat() != GdPictureStatus.OK) break;

            if (string.IsNullOrEmpty(fieldTitle)) continue;
            if (!fieldValues.TryGetValue(fieldTitle, out var value)) continue;

            try
            {
                pdf.SetFormFieldValue(fieldId, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not set form field {Field}", fieldTitle);
            }
        }

        using var outputStream = new MemoryStream();
        pdf.SaveToStream(outputStream, false, false);
        return outputStream.ToArray();
    }

    private string? ResolveTemplatePath(string templateId)
    {
        if (string.IsNullOrWhiteSpace(_options.TemplatePath)) return null;
        var basePath = _options.TemplatePath.TrimEnd('/', '\\');
        var safeId = Path.GetFileName(templateId);
        if (!safeId.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            safeId += ".pdf";
        return Path.Combine(basePath, safeId);
    }

    private bool IsCalculatorTemplate(string templateId)
    {
        var ids = _options.CalculatorTemplateIds?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var baseId = Path.GetFileNameWithoutExtension(templateId);
        return ids.Any(id => string.Equals(id, baseId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Pre-calculate Kt/V and URR from input fields. Supported inputs: K, t, V (for Kt/V); PreUrea, PostUrea (for URR). Outputs: KtV, URR.</summary>
    private static void PreCalculateAdequacy(Dictionary<string, string> fieldValues)
    {
        if (TryParseDouble(fieldValues, "K", out var k) && TryParseDouble(fieldValues, "t", out var t) && TryParseDouble(fieldValues, "V", out var v) && v > 0)
        {
            var ktv = (k * t) / v;
            fieldValues["KtV"] = ktv.ToString("F2");
        }
        if (TryParseDouble(fieldValues, "PreUrea", out var preUrea) && TryParseDouble(fieldValues, "PostUrea", out var postUrea) && preUrea > 0)
        {
            var urr = (preUrea - postUrea) / preUrea * 100;
            fieldValues["URR"] = urr.ToString("F1");
        }
    }

    private static bool TryParseDouble(Dictionary<string, string> dict, string key, out double value)
    {
        value = 0;
        if (!dict.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s)) return false;
        return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}

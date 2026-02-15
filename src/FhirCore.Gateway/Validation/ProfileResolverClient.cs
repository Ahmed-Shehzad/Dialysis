using System.Text;
using Hl7.Fhir.Model;

namespace FhirCore.Gateway.Validation;

public sealed class ProfileResolverClient
{
    private readonly FhirValidatorService _validatorService;

    public ProfileResolverClient(FhirValidatorService validatorService)
    {
        _validatorService = validatorService;
    }

    public async Task<(ValidationResult Result, Resource? Resource)> ValidateRequestAsync(HttpRequest request)
    {
        if (request.Method is not "POST" and not "PUT")
        {
            return (new ValidationResult(true, []), null);
        }

        request.EnableBuffering();
        string json;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
        {
            json = await reader.ReadToEndAsync();
        }
        request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(json))
        {
            return (new ValidationResult(false, ["Request body is required for POST/PUT."]), null);
        }

        var (resource, parseError) = _validatorService.TryParseResource(json);
        if (resource is null)
        {
            return (new ValidationResult(false, [parseError ?? "Invalid FHIR JSON."]), null);
        }

        var profile = resource.Meta?.Profile?.FirstOrDefault();
        var outcome = _validatorService.Validate(resource, profile);

        var errors = outcome.Issue
            .Where(i => i.Severity == OperationOutcome.IssueSeverity.Error || i.Severity == OperationOutcome.IssueSeverity.Fatal)
            .Select(i => i.Diagnostics ?? i.Details?.Text ?? i.Code?.ToString() ?? "Validation error")
            .ToList();

        return (new ValidationResult(errors.Count == 0, errors), resource);
    }

    public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
}

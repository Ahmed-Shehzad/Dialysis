using Dialysis.Contracts.Events;
using Dialysis.Contracts.Ids;
using Hl7.Fhir.Model;
using Transponder.Abstractions;

namespace FhirCore.Gateway.Validation;

public sealed class FhirValidationMiddleware
{
    private readonly RequestDelegate _next;

    public FhirValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async System.Threading.Tasks.Task InvokeAsync(
        HttpContext context,
        ProfileResolverClient profileResolver,
        OperationOutcomeWriter outcomeWriter,
        IPublishEndpoint publishEndpoint)
    {
        if (!context.Request.Path.StartsWithSegments("/fhir", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var (validationResult, resource) = await profileResolver.ValidateRequestAsync(context.Request);
        if (!validationResult.IsValid)
        {
            await outcomeWriter.WriteAsync(context.Response, validationResult.Errors);
            return;
        }

        await _next(context);

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            var resourceType = ExtractResourceTypeFromPath(context.Request.Path);
            var resourceId = resource?.Id ?? ExtractIdFromLocation(context.Response.Headers["Location"]) ?? ExtractIdFromPath(context.Request.Path);

            if (!string.IsNullOrEmpty(resourceType) && !string.IsNullOrEmpty(resourceId))
            {
                var searchContext = BuildSearchContext(resourceType, resource);
                var correlationId = Ulid.NewUlid();
                await publishEndpoint.PublishAsync(new ResourceWrittenEvent(
                    correlationId,
                    resourceType,
                    resourceId,
                    "1",
                    DateTimeOffset.UtcNow,
                    searchContext
                ), context.RequestAborted);
            }

            if (resource is Observation obs)
            {
                var id = obs.Id ?? resourceId;
                var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var evt = MapToObservationCreated(obs, id ?? "", tenantId);
                if (evt is not null)
                {
                    await publishEndpoint.PublishAsync(evt, context.RequestAborted);
                }
            }
        }
    }

    private static string? ExtractIdFromPath(PathString path)
    {
        var segments = path.Value?.Trim('/').Split('/') ?? [];
        return segments.Length >= 3 && segments[0].Equals("fhir", StringComparison.OrdinalIgnoreCase)
            ? segments[2]
            : null;
    }

    private static IReadOnlyDictionary<string, string>? BuildSearchContext(string resourceType, Resource? resource)
    {
        if (resource is null) return null;
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (resource is Observation obs && !string.IsNullOrEmpty(obs.Subject?.Reference))
        {
            dict["subject"] = obs.Subject.Reference;
            var parts = obs.Subject.Reference.Split('/');
            var patientId = parts.Length > 1 ? parts[^1].Trim() : obs.Subject.Reference.Trim();
            if (!string.IsNullOrEmpty(patientId)) dict["patient"] = patientId;
        }
        else if (resource is Patient p && !string.IsNullOrEmpty(p.Id))
            dict["_id"] = p.Id;
        else if (resource is Encounter enc && !string.IsNullOrEmpty(enc.Subject?.Reference))
            dict["subject"] = enc.Subject.Reference;
        return dict.Count > 0 ? dict : null;
    }

    private static string? ExtractResourceTypeFromPath(PathString path)
    {
        var segments = path.Value?.Trim('/').Split('/') ?? [];
        return segments.Length >= 2 && segments[0].Equals("fhir", StringComparison.OrdinalIgnoreCase)
            ? segments[1]
            : null;
    }

    private static string ExtractIdFromLocation(string? location)
    {
        if (string.IsNullOrEmpty(location))
        {
            return "";
        }
        var segments = location.TrimEnd('/').Split('/');
        return segments.Length >= 2 ? segments[^1] : "";
    }

    private static ObservationCreated? MapToObservationCreated(Observation obs, string id, string? tenantId)
    {
        var patientId = obs.Subject?.Reference?.Replace("Patient/", "") ?? "";
        var encounterId = obs.Encounter?.Reference?.Replace("Encounter/", "") ?? "";
        var code = obs.Code?.Coding?.FirstOrDefault()?.Code ?? obs.Code?.Text ?? "";
        var value = obs.Value is Quantity q ? q.Value?.ToString() ?? "" : obs.Value?.ToString() ?? "";
        var effective = obs.Effective switch
        {
            Hl7.Fhir.Model.FhirDateTime fd => DateTimeOffset.TryParse(fd.Value, out var dt) ? dt : DateTimeOffset.MinValue,
            Hl7.Fhir.Model.Period p when p.Start != null => DateTimeOffset.TryParse(p.Start, out var dt) ? dt : DateTimeOffset.MinValue,
            _ => DateTimeOffset.MinValue
        };
        var deviceId = obs.Device?.Reference?.Replace("Device/", "");

        if (string.IsNullOrEmpty(patientId) || string.IsNullOrEmpty(encounterId))
            return null;

        var observationId = ObservationId.Create(string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N")[..24] : id);
        var pId = PatientId.Create(patientId);
        var eId = EncounterId.Create(encounterId);

        var correlationId = Ulid.NewUlid();
        return new ObservationCreated(
            correlationId,
            string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim(),
            observationId,
            pId,
            eId,
            code,
            value,
            effective,
            deviceId
        );
    }
}

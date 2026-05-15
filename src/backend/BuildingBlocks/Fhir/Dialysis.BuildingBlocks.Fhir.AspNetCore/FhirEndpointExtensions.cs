using System.Text;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

public static class FhirEndpointExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Wires the FHIR endpoint surface at the configured base URL:
        /// <list type="bullet">
        ///   <item><description><c>GET {BaseUrl}/metadata</c> — CapabilityStatement</description></item>
        ///   <item><description><c>GET {BaseUrl}/{resourceType}/{id}</c> — single resource read</description></item>
        ///   <item><description><c>GET {BaseUrl}/{resourceType}</c> — type-level search returning a Bundle</description></item>
        /// </list>
        /// </summary>
        public IEndpointRouteBuilder MapFhirEndpoints(
            Action<FhirEndpointOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var options = new FhirEndpointOptions();
            configure?.Invoke(options);
            var resolved = endpoints.ServiceProvider.GetService<IOptions<FhirEndpointOptions>>()?.Value;
            if (resolved is not null)
            {
                options.BaseUrl = resolved.BaseUrl;
                options.PublicBaseUrl = resolved.PublicBaseUrl;
                options.RespectFormatQueryParameter = resolved.RespectFormatQueryParameter;
            }

            var baseUrl = options.BaseUrl.TrimEnd('/');

            endpoints.MapGet(baseUrl + "/metadata", HandleMetadataAsync);
            endpoints.MapGet(baseUrl + "/{resourceType}/{id}", HandleReadAsync);
            endpoints.MapGet(baseUrl + "/{resourceType}", HandleSearchAsync);

            return endpoints;
        }
    }

    private static async Task HandleMetadataAsync(HttpContext context)
    {
        var capabilityProvider = context.RequestServices.GetRequiredService<IFhirCapabilityProvider>();
        var serializer = context.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();

        var statement = new CapabilityStatement
        {
            Status = PublicationStatus.Active,
            Date = DateTimeOffset.UtcNow.ToString("O"),
            Kind = CapabilityStatementKind.Instance,
            FhirVersion = FHIRVersion.N4_0_1,
            Format = [FhirContentTypes.Json],
            Rest =
            [
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Resource = [.. capabilityProvider.DescribeResources()],
                },
            ],
        };

        await WriteFhirResponseAsync(context, statement, serializer, StatusCodes.Status200OK).ConfigureAwait(false);
    }

    private static async Task HandleReadAsync(HttpContext context)
    {
        var registry = context.RequestServices.GetRequiredService<FhirResourceRegistry>();
        var serializer = context.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();
        var resourceType = (string)context.Request.RouteValues["resourceType"]!;
        var id = (string)context.Request.RouteValues["id"]!;

        if (!registry.TryGetReadDispatcher(resourceType, out var dispatcher))
        {
            await WriteFhirResponseAsync(
                context,
                OperationOutcomeFactory.NotSupported($"Read is not supported for resource type '{resourceType}'."),
                serializer,
                StatusCodes.Status404NotFound).ConfigureAwait(false);
            return;
        }

        var consentGate = context.RequestServices.GetRequiredService<IFhirConsentGate>();
        var consentDecision = await consentGate.EvaluateAsync(
            new FhirConsentContext(resourceType, id, PatientId: null, RequestorId: context.User.Identity?.Name),
            context.RequestAborted).ConfigureAwait(false);

        if (!consentDecision.Permitted)
        {
            await WriteFhirResponseAsync(
                context,
                OperationOutcomeFactory.Forbidden(consentDecision.Reason ?? "Consent denied."),
                serializer,
                StatusCodes.Status403Forbidden).ConfigureAwait(false);
            return;
        }

        var result = await dispatcher(context.RequestServices, id, context.RequestAborted).ConfigureAwait(false);
        if (result.Resource is null)
        {
            await WriteFhirResponseAsync(
                context,
                OperationOutcomeFactory.NotFound(resourceType, id),
                serializer,
                StatusCodes.Status404NotFound).ConfigureAwait(false);
            return;
        }

        if (result.VersionId is not null)
        {
            context.Response.Headers.ETag = $"W/\"{result.VersionId}\"";
        }
        if (result.LastModified is not null)
        {
            context.Response.Headers.LastModified = result.LastModified.Value.ToString("R");
        }

        await WriteFhirResponseAsync(context, result.Resource, serializer, StatusCodes.Status200OK).ConfigureAwait(false);
    }

    private static async Task HandleSearchAsync(HttpContext context)
    {
        var registry = context.RequestServices.GetRequiredService<FhirResourceRegistry>();
        var serializer = context.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();
        var resourceType = (string)context.Request.RouteValues["resourceType"]!;

        if (!registry.TryGetSearchDispatcher(resourceType, out var dispatcher))
        {
            await WriteFhirResponseAsync(
                context,
                OperationOutcomeFactory.NotSupported($"Search is not supported for resource type '{resourceType}'."),
                serializer,
                StatusCodes.Status404NotFound).ConfigureAwait(false);
            return;
        }

        var parameters = (IReadOnlyDictionary<string, string[]>)context.Request.Query
            .Where(q => !q.Key.StartsWith('_') || q.Key is "_id" or "_lastUpdated" or "_count" or "_sort")
            .ToDictionary(
                q => q.Key,
                q => q.Value.Where(v => v is not null).Select(v => v!).ToArray(),
                StringComparer.Ordinal);

        int? count = null;
        if (context.Request.Query.TryGetValue("_count", out var countValue)
            && int.TryParse(countValue, out var parsedCount))
        {
            count = parsedCount;
        }

        var request = new FhirSearchRequest(resourceType, parameters, count);
        var bundle = await dispatcher(context.RequestServices, request, context.RequestAborted).ConfigureAwait(false);
        await WriteFhirResponseAsync(context, bundle, serializer, StatusCodes.Status200OK).ConfigureAwait(false);
    }

    internal static async Task WriteFhirResponseAsync(
        HttpContext context,
        Base resource,
        FhirJsonSerializerProvider serializer,
        int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = FhirContentTypes.Json + "; charset=utf-8";
        var json = serializer.Serialize(resource);
        var bytes = Encoding.UTF8.GetBytes(json);
        await context.Response.Body.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
    }
}

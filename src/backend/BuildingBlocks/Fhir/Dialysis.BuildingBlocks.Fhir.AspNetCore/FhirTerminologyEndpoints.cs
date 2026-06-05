using System.Text;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Dialysis.BuildingBlocks.Fhir.Terminology;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

/// <summary>
/// FHIR R4 terminology operations over the governed <see cref="DialysisTerminologyCatalog"/>:
/// <c>$validate-code</c>, <c>$translate</c>, <c>$expand</c>, <c>$lookup</c>, plus a governance listing
/// of the platform's canonical resources. These let LIS coding, imaging-AI findings, and cross-context
/// validators check codes against the platform's value sets / concept maps without an upstream tx server.
/// </summary>
public static class FhirTerminologyEndpoints
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the terminology operations under <paramref name="baseUrl"/> (default <c>/fhir</c>):
        /// <list type="bullet">
        ///   <item><description><c>GET {base}/ValueSet/$validate-code?url=&amp;code=&amp;system=</c></description></item>
        ///   <item><description><c>GET {base}/ValueSet/$expand?url=</c></description></item>
        ///   <item><description><c>GET {base}/ConceptMap/$translate?url=&amp;system=&amp;code=</c></description></item>
        ///   <item><description><c>GET {base}/CodeSystem/$lookup?system=&amp;code=</c></description></item>
        ///   <item><description><c>GET {base}/_terminology/catalog</c> — governed canonical-resource inventory</description></item>
        /// </list>
        /// Requires <see cref="FhirTerminologyServiceCollectionExtensions"/>'s <c>AddDialysisTerminologyCatalog</c>.
        /// </summary>
        public IEndpointRouteBuilder MapFhirTerminologyEndpoints(string baseUrl = "/fhir")
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            var root = baseUrl.TrimEnd('/');

            endpoints.MapGet(root + "/ValueSet/$validate-code", ValidateCodeAsync);
            endpoints.MapGet(root + "/ValueSet/$expand", ExpandAsync);
            endpoints.MapGet(root + "/ConceptMap/$translate", TranslateAsync);
            endpoints.MapGet(root + "/CodeSystem/$lookup", LookupAsync);
            endpoints.MapGet(root + "/_terminology/catalog", ListCatalog);
            return endpoints;
        }
    }

    private static async Task ValidateCodeAsync(HttpContext context, string? url, string? code, string? system)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(code))
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest, "url and code are required.").ConfigureAwait(false);
            return;
        }
        var catalog = context.RequestServices.GetRequiredService<DialysisTerminologyCatalog>();
        var result = await catalog.Service.ValidateCodeAsync(url, code, system, context.RequestAborted).ConfigureAwait(false);
        await WriteResourceAsync(context, result, StatusCodes.Status200OK).ConfigureAwait(false);
    }

    private static async Task ExpandAsync(HttpContext context, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest, "url is required.").ConfigureAwait(false);
            return;
        }
        var catalog = context.RequestServices.GetRequiredService<DialysisTerminologyCatalog>();
        var expanded = await catalog.Service.ExpandAsync(url, new Dictionary<string, string>(), context.RequestAborted).ConfigureAwait(false);
        await WriteResourceAsync(context, expanded, StatusCodes.Status200OK).ConfigureAwait(false);
    }

    private static async Task TranslateAsync(HttpContext context, string? url, string? system, string? code)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(system) || string.IsNullOrWhiteSpace(code))
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest, "url, system and code are required.").ConfigureAwait(false);
            return;
        }
        var catalog = context.RequestServices.GetRequiredService<DialysisTerminologyCatalog>();
        var result = await catalog.Service.TranslateAsync(url, system, code, context.RequestAborted).ConfigureAwait(false);
        await WriteResourceAsync(context, result, StatusCodes.Status200OK).ConfigureAwait(false);
    }

    private static async Task LookupAsync(HttpContext context, string? system, string? code)
    {
        if (string.IsNullOrWhiteSpace(system) || string.IsNullOrWhiteSpace(code))
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest, "system and code are required.").ConfigureAwait(false);
            return;
        }
        var catalog = context.RequestServices.GetRequiredService<DialysisTerminologyCatalog>();
        var result = await catalog.Service.LookupAsync(system, code, context.RequestAborted).ConfigureAwait(false);
        await WriteResourceAsync(context, result, StatusCodes.Status200OK).ConfigureAwait(false);
    }

    private static IResult ListCatalog(HttpContext context)
    {
        var catalog = context.RequestServices.GetRequiredService<DialysisTerminologyCatalog>();
        return Results.Json(catalog.Resources);
    }

    private static async Task WriteResourceAsync(HttpContext context, Resource resource, int status)
    {
        var serializer = context.RequestServices.GetRequiredService<FhirJsonSerializerProvider>();
        context.Response.StatusCode = status;
        context.Response.ContentType = FhirContentTypes.Json + "; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(resource, pretty: true));
        await context.Response.Body.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
    }

    private static Task WriteOutcomeAsync(HttpContext context, int status, string message)
    {
        var outcome = new OperationOutcome();
        outcome.Issue.Add(new OperationOutcome.IssueComponent
        {
            Severity = OperationOutcome.IssueSeverity.Error,
            Code = OperationOutcome.IssueType.Invalid,
            Diagnostics = message,
        });
        return WriteResourceAsync(context, outcome, status);
    }
}

using System.Text;
using System.Text.Json;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Dialysis.BuildingBlocks.Fhir.Validation.Authoring;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

/// <summary>
/// On-demand FHIR authoring surface. Clients POST a declarative spec; the host builds the
/// StructureDefinition / ImplementationGuide, verifies its correctness (snapshot + round-trip),
/// publishes it into the conformance registry only when valid, and returns a Bundle whose first
/// entry is the verification <c>OperationOutcome</c>.
/// </summary>
public static class FhirAuthoringEndpoints
{
    private static readonly JsonSerializerOptions _webJson = new(JsonSerializerDefaults.Web);

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the authoring routes under <paramref name="baseUrl"/> (default <c>/fhir/_author</c>):
        /// <list type="bullet">
        ///   <item><description><c>POST {base}/StructureDefinition</c> — build+verify a profile from a <see cref="FhirProfileSpec"/></description></item>
        ///   <item><description><c>POST {base}/ImplementationGuide</c> — build+verify an IG from a <see cref="FhirImplementationGuideSpec"/></description></item>
        ///   <item><description><c>GET {base}/StructureDefinition</c> / <c>/{id}</c> — list / read published profiles</description></item>
        ///   <item><description><c>GET {base}/ImplementationGuide</c> — list published IGs</description></item>
        /// </list>
        /// </summary>
        public IEndpointRouteBuilder MapFhirAuthoringEndpoints(string baseUrl = "/fhir/_author")
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            var root = baseUrl.TrimEnd('/');

            endpoints.MapPost(root + "/StructureDefinition", AuthorProfileAsync);
            endpoints.MapPost(root + "/ImplementationGuide", AuthorGuideAsync);
            endpoints.MapPost(root + "/package", LoadPackageAsync);
            endpoints.MapGet(root + "/StructureDefinition", ListProfilesAsync);
            endpoints.MapGet(root + "/StructureDefinition/{id}", ReadProfileAsync);
            endpoints.MapGet(root + "/ImplementationGuide", ListGuidesAsync);

            return endpoints;
        }
    }

    private static async Task AuthorProfileAsync(HttpContext context)
    {
        var authoring = context.RequestServices.GetRequiredService<IFhirArtifactAuthoringService>();
        var ct = context.RequestAborted;

        FhirProfileSpec? spec;
        try
        {
            spec = await JsonSerializer
                .DeserializeAsync<FhirProfileSpec>(context.Request.Body, _webJson, ct)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest,
                $"Request body is not a valid FhirProfileSpec: {ex.Message}").ConfigureAwait(false);
            return;
        }

        if (spec is null)
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest,
                "Request body is empty.").ConfigureAwait(false);
            return;
        }

        try
        {
            var result = await authoring.AuthorProfileAsync(spec, ct).ConfigureAwait(false);
            var bundle = Wrap(result.Verification.Outcome, result.Profile);
            await WriteBundleAsync(context, bundle,
                result.Published ? StatusCodes.Status201Created : StatusCodes.Status422UnprocessableEntity)
                .ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest, ex.Message)
                .ConfigureAwait(false);
        }
    }

    private static async Task AuthorGuideAsync(HttpContext context)
    {
        var authoring = context.RequestServices.GetRequiredService<IFhirArtifactAuthoringService>();
        var ct = context.RequestAborted;

        FhirImplementationGuideSpec? spec;
        try
        {
            spec = await JsonSerializer
                .DeserializeAsync<FhirImplementationGuideSpec>(context.Request.Body, _webJson, ct)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest,
                $"Request body is not a valid FhirImplementationGuideSpec: {ex.Message}").ConfigureAwait(false);
            return;
        }

        if (spec is null)
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest,
                "Request body is empty.").ConfigureAwait(false);
            return;
        }

        try
        {
            var result = await authoring.AuthorImplementationGuideAsync(spec, ct).ConfigureAwait(false);
            var bundle = Wrap(result.Verification.Outcome, result.Guide);
            foreach (var profile in result.Profiles)
                bundle.Entry.Add(new Bundle.EntryComponent { Resource = profile });
            await WriteBundleAsync(context, bundle,
                result.Published ? StatusCodes.Status201Created : StatusCodes.Status422UnprocessableEntity)
                .ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest, ex.Message)
                .ConfigureAwait(false);
        }
    }

    private static async Task LoadPackageAsync(HttpContext context)
    {
        var loader = context.RequestServices.GetRequiredService<IFhirPackageLoader>();
        var ct = context.RequestAborted;

        try
        {
            var result = await loader.LoadAsync(context.Request.Body, ct).ConfigureAwait(false);
            var outcome = new OperationOutcome();
            outcome.Issue.Add(new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Information,
                Code = OperationOutcome.IssueType.Informational,
                Diagnostics =
                    $"Loaded package {result.PackageName ?? "(unknown)"}@{result.PackageVersion ?? "?"}: " +
                    $"{result.Loaded} conformance resource(s) registered, {result.Skipped} skipped.",
            });
            foreach (var canonical in result.Canonicals.Take(200))
            {
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Information,
                    Code = OperationOutcome.IssueType.Informational,
                    Diagnostics = $"registered {canonical}",
                });
            }

            await WriteResourceAsync(
                context, outcome,
                result.Loaded > 0 ? StatusCodes.Status200OK : StatusCodes.Status422UnprocessableEntity)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or ArgumentException)
        {
            await WriteOutcomeAsync(context, StatusCodes.Status400BadRequest,
                $"Could not read FHIR package tarball: {ex.Message}").ConfigureAwait(false);
        }
    }

    private static Task ListProfilesAsync(HttpContext context)
    {
        var registry = context.RequestServices.GetRequiredService<IFhirConformanceRegistry>();
        var bundle = new Bundle { Type = Bundle.BundleType.Searchset };
        foreach (var sd in registry.Profiles)
            bundle.Entry.Add(new Bundle.EntryComponent { Resource = sd });
        bundle.Total = registry.Profiles.Count;
        return WriteBundleAsync(context, bundle, StatusCodes.Status200OK);
    }

    private static Task ReadProfileAsync(HttpContext context)
    {
        var registry = context.RequestServices.GetRequiredService<IFhirConformanceRegistry>();
        var id = (string?)context.Request.RouteValues["id"];

        var match = registry.Profiles.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.Ordinal) ||
            string.Equals(p.Url, id, StringComparison.Ordinal));

        if (match is null)
        {
            return WriteOutcomeAsync(context, StatusCodes.Status404NotFound,
                $"No authored StructureDefinition matches '{id}'.");
        }

        return WriteResourceAsync(context, match, StatusCodes.Status200OK);
    }

    private static Task ListGuidesAsync(HttpContext context)
    {
        var registry = context.RequestServices.GetRequiredService<IFhirConformanceRegistry>();
        var bundle = new Bundle { Type = Bundle.BundleType.Searchset };
        foreach (var ig in registry.ImplementationGuides)
            bundle.Entry.Add(new Bundle.EntryComponent { Resource = ig });
        bundle.Total = registry.ImplementationGuides.Count;
        return WriteBundleAsync(context, bundle, StatusCodes.Status200OK);
    }

    private static Bundle Wrap(OperationOutcome outcome, Resource artifact)
    {
        var bundle = new Bundle { Type = Bundle.BundleType.Collection };
        bundle.Entry.Add(new Bundle.EntryComponent { Resource = outcome });
        bundle.Entry.Add(new Bundle.EntryComponent { Resource = artifact });
        return bundle;
    }

    private static Task WriteBundleAsync(
        HttpContext context, Bundle bundle, int status)
        => WriteResourceAsync(context, bundle, status);

    private static async Task WriteResourceAsync(
        HttpContext context, Resource resource, int status)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = FhirContentTypes.Json + "; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(FhirJsonSerializerProvider.Serialize(resource, pretty: true));
        await context.Response.Body.WriteAsync(bytes, context.RequestAborted).ConfigureAwait(false);
    }

    private static Task WriteOutcomeAsync(
        HttpContext context, int status, string message)
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

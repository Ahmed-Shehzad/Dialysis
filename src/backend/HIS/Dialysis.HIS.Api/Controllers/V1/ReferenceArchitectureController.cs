using Asp.Versioning;
using Dialysis.HIS.Api.Hateoas;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>
/// Describes how this host maps to the six top-level RA modules in Tummers et al. (2021), BMC Med Inform Decis Mak —
/// <see href="https://doi.org/10.1186/s12911-021-01570-2">DOI 10.1186/s12911-021-01570-2</see> (see also <c>docs/book/s12911-021-01570-2.pdf</c>).
/// Per-sub-module (Fig. 6, 34 boxes) traceability: <c>Dialysis.HIS/his_ra_submodules.md</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reference-architecture")]
public sealed class ReferenceArchitectureController : HisHateoasControllerBase
{
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(ResourceEnvelope<ReferenceArchitectureCatalogDto>), StatusCodes.Status200OK)]
    public IActionResult GetCatalog()
    {
        var catalog = new ReferenceArchitectureCatalogDto(
            Paper: "Tummers et al. (2021) — Designing a reference architecture for health information systems",
            Doi: "https://doi.org/10.1186/s12911-021-01570-2",
            Modules:
            [
                new RaModuleDto(
                    "Security",
                    "Authentication, authorization, audit mechanisms; vertical security across layers.",
                    "api/v{version}/security/*",
                    Implemented: true),
                new RaModuleDto(
                    "PlanningAndScheduling",
                    "Appointments, calendars, resource assignment, waitlists.",
                    "api/v{version}/scheduling/*; api/v{version}/reference-architecture/capabilities/planning-and-scheduling/waitlists",
                    Implemented: true),
                new RaModuleDto(
                    "PatientMonitoring",
                    "ADT, referrals, status, EHR-facing events, patient portal, device/telemetry ingest.",
                    "api/v{version}/patient-flow/*, patient-portal/*, integration/device-readings; api/v{version}/reference-architecture/capabilities/patient-monitoring/*",
                    Implemented: true),
                new RaModuleDto(
                    "MedicationManagement",
                    "Medication orders, administration documentation, pharmacy-facing safety.",
                    "api/v{version}/medication/*; api/v{version}/reference-architecture/capabilities/medication-management/*",
                    Implemented: true),
                new RaModuleDto(
                    "GenericMis",
                    "Staff/assets, internal communications, quality, financial/billing.",
                    "api/v{version}/operations/*; api/v{version}/reference-architecture/capabilities/generic-mis/*",
                    Implemented: true),
                new RaModuleDto(
                    "DataManagement",
                    "Import/export, structured search, analytics, reporting.",
                    "api/v{version}/data-management/*; api/v{version}/reference-architecture/capabilities/data-management/*",
                    Implemented: true),
            ]);
        return OkResource(catalog);
    }

    public sealed record ReferenceArchitectureCatalogDto
    {
        public ReferenceArchitectureCatalogDto(string Paper,
            string Doi,
            IReadOnlyList<RaModuleDto> Modules)
        {
            this.Paper = Paper;
            this.Doi = Doi;
            this.Modules = Modules;
        }
        public string Paper { get; init; }
        public string Doi { get; init; }
        public IReadOnlyList<RaModuleDto> Modules { get; init; }
        public void Deconstruct(out string Paper, out string Doi, out IReadOnlyList<RaModuleDto> Modules)
        {
            Paper = this.Paper;
            Doi = this.Doi;
            Modules = this.Modules;
        }
    }

    public sealed record RaModuleDto
    {
        public RaModuleDto(string Code,
            string Summary,
            string ApiRoutePrefix,
            bool Implemented)
        {
            this.Code = Code;
            this.Summary = Summary;
            this.ApiRoutePrefix = ApiRoutePrefix;
            this.Implemented = Implemented;
        }
        public string Code { get; init; }
        public string Summary { get; init; }
        public string ApiRoutePrefix { get; init; }
        public bool Implemented { get; init; }
        public void Deconstruct(out string Code, out string Summary, out string ApiRoutePrefix, out bool Implemented)
        {
            Code = this.Code;
            Summary = this.Summary;
            ApiRoutePrefix = this.ApiRoutePrefix;
            Implemented = this.Implemented;
        }
    }
}

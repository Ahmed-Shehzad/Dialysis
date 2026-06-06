using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Hl7.Fhir.Model;
using DomainCarePlan = Dialysis.EHR.PatientChart.Domain.CarePlan;
using FhirCarePlan = Hl7.Fhir.Model.CarePlan;

namespace Dialysis.EHR.PatientChart.Fhir;

/// <summary>
/// Streams every <c>CarePlan</c> aggregate as a FHIR R4 <c>CarePlan</c> (<c>intent = plan</c>). Each
/// trackable goal is carried as a <c>CarePlan.activity.detail</c> (description + status + optional
/// target measure) so the plan exports as a single self-contained resource — a separate <c>Goal</c>
/// feeder is a future refinement. The plan's creation timestamp drives <c>Meta.lastUpdated</c> and the
/// incremental (<c>_since</c>) export filter.
/// </summary>
public sealed class EhrCarePlanFeeder : INdjsonResourceFeeder<FhirCarePlan>
{
    private readonly ICarePlanRepository _carePlans;
    public EhrCarePlanFeeder(ICarePlanRepository carePlans) => _carePlans = carePlans;

    public async IAsyncEnumerable<FhirCarePlan> StreamAsync(
        ExportJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await foreach (var plan in _carePlans.StreamAllAsync(job.Since, cancellationToken).ConfigureAwait(false))
        {
            yield return Project(plan);
        }
    }

    private static FhirCarePlan Project(DomainCarePlan source) => new()
    {
        Id = source.Id.ToString(),
        Meta = new Meta { LastUpdated = new DateTimeOffset(source.CreatedAtUtc, TimeSpan.Zero) },
        Status = MapStatus(source.Status),
        Intent = FhirCarePlan.CarePlanIntent.Plan,
        Title = source.Title,
        Subject = new ResourceReference($"Patient/{source.PatientId}"),
        Author = new ResourceReference($"Practitioner/{source.AuthoredByProviderId}"),
        CreatedElement = new FhirDateTime(new DateTimeOffset(source.CreatedAtUtc, TimeSpan.Zero)),
        Activity =
        [
            .. source.Goals.Select(g => new FhirCarePlan.ActivityComponent
            {
                Detail = new FhirCarePlan.DetailComponent
                {
                    Description = g.Description,
                    Status = MapGoalStatus(g.Status),
                    Code = string.IsNullOrWhiteSpace(g.TargetMeasure)
                        ? null
                        : new CodeableConcept { Text = g.TargetMeasure },
                },
            }),
        ],
    };

    private static RequestStatus? MapStatus(CarePlanStatus status) => status switch
    {
        CarePlanStatus.Active => RequestStatus.Active,
        CarePlanStatus.Completed => RequestStatus.Completed,
        CarePlanStatus.Revoked => RequestStatus.Revoked,
        _ => null,
    };

    private static FhirCarePlan.CarePlanActivityStatus MapGoalStatus(CarePlanGoalStatus status) => status switch
    {
        CarePlanGoalStatus.Proposed => FhirCarePlan.CarePlanActivityStatus.NotStarted,
        CarePlanGoalStatus.InProgress => FhirCarePlan.CarePlanActivityStatus.InProgress,
        CarePlanGoalStatus.Achieved => FhirCarePlan.CarePlanActivityStatus.Completed,
        CarePlanGoalStatus.NotAchieved => FhirCarePlan.CarePlanActivityStatus.Stopped,
        _ => FhirCarePlan.CarePlanActivityStatus.Unknown,
    };
}

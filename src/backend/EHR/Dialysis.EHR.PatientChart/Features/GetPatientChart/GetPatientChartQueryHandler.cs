using System.Globalization;
using Dialysis.CQRS.Queries;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.GetPatientChart;

public sealed class GetPatientChartQueryHandler(
    IAllergyRepository allergies,
    IProblemListRepository problems,
    IMedicationStatementRepository medications,
    IVitalSignRepository vitals,
    IImmunizationRepository immunizations)
    : IQueryHandler<GetPatientChartQuery, PatientChartView>
{
    public async Task<PatientChartView> HandleAsync(GetPatientChartQuery request, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddYears(-5);

        var allergyList = await allergies.ListByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        var problemList = await problems.ListByPatientAsync(request.PatientId, false, cancellationToken).ConfigureAwait(false);
        var medList = await medications.ListByPatientAsync(request.PatientId, false, cancellationToken).ConfigureAwait(false);
        var vitalsList = await vitals.ListByPatientAsync(request.PatientId, since, cancellationToken).ConfigureAwait(false);
        var immunList = await immunizations.ListByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);

        return new PatientChartView(
            request.PatientId,
            allergyList.Select(a => new PatientChartItem(
                "Allergy", a.Id, DateTime.UtcNow, a.Allergen.Code, a.Allergen.Display ?? a.Allergen.Code,
                a.ReactionText, a.VerificationStatus.ToString())).ToList(),
            problemList.Select(p => new PatientChartItem(
                "Problem", p.Id, p.OnsetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                p.Condition.Code, p.Condition.Display ?? p.Condition.Code, p.Notes, p.Status.ToString())).ToList(),
            medList.Select(m => new PatientChartItem(
                "Medication", m.Id, m.StartedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                m.Medication.Code, m.Medication.Display ?? m.Medication.Code,
                $"{m.DoseText} · {m.FrequencyText}", m.Status.ToString())).ToList(),
            vitalsList.Select(v => new PatientChartItem(
                "Vital", v.Id, v.ObservedAtUtc,
                v.ObservationType.Code, v.ObservationType.Display ?? v.ObservationType.Code,
                $"{v.Value.ToString(CultureInfo.InvariantCulture)} {v.UnitCode}", null)).ToList(),
            immunList.Select(i => new PatientChartItem(
                "Immunization", i.Id, i.AdministeredOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                i.Vaccine.Code, i.Vaccine.Display ?? i.Vaccine.Code, i.SiteCode, i.Status.ToString())).ToList());
    }
}

namespace Dialysis.Gateway.Features.Patients;

public sealed record ListPatientsResponse(IReadOnlyList<PatientResponse> Patients);

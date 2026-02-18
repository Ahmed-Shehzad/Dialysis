using Dialysis.Patient.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.SearchPatients;

public sealed record SearchPatientsQuery(Person Name) : IQuery<SearchPatientsResponse>;

using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.DeIdentification;

public enum DeIdentificationProfile
{
    SafeHarbor,
    LimitedDataSet,
    Custom,
}

public interface IFhirDeIdentifier
{
    Resource Apply(Resource resource, DeIdentificationProfile profile);
}

public interface IDateShiftProvider
{
    /// <summary>Per-patient consistent offset in days. ±30 days default range.</summary>
    int GetShiftDaysForPatient(string patientId);
}

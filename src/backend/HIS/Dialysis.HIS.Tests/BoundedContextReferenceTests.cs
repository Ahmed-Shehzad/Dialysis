using Dialysis.HIS.DataServices.Features.ManagerDashboard;
using Dialysis.HIS.Integration;
using Dialysis.HIS.Medication.Features.PlaceMedicationOrder;
using Dialysis.HIS.Scheduling.Features.BookAppointment;

namespace Dialysis.HIS.Tests;

/// <summary>Guards modular monolith boundaries: bounded-context assemblies should not take direct project references on other contexts' domain packages.</summary>
public sealed class BoundedContextReferenceTests
{
    [Fact]
    public void Scheduling_assembly_does_not_reference_PatientFlow()
    {
        var scheduling = typeof(BookAppointmentCommand).Assembly;
        var refNames = scheduling.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Dialysis.HIS.PatientFlow", refNames);
    }

    [Fact]
    public void Medication_assembly_does_not_reference_PatientFlow()
    {
        var medication = typeof(PlaceMedicationOrderCommand).Assembly;
        var refNames = medication.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Dialysis.HIS.PatientFlow", refNames);
    }

    [Fact]
    public void DataServices_assembly_does_not_reference_PatientFlow()
    {
        var dataServices = typeof(ManagerDashboardQuery).Assembly;
        var refNames = dataServices.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Dialysis.HIS.PatientFlow", refNames);
    }

    [Fact]
    public void Integration_assembly_does_not_reference_RaCapabilities()
    {
        var integration = typeof(HisIntegrationMarker).Assembly;
        var refNames = integration.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Dialysis.HIS.RaCapabilities", refNames);
    }
}

using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Configurations;

/// <summary>
/// Composite entry point — invokes per-bounded-context configuration so the <see cref="EhrDbContext"/>
/// only needs a single line in <c>OnModelCreating</c>.
/// </summary>
internal static class EhrModelConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        RegistrationConfiguration.Configure(modelBuilder);
        PatientChartConfiguration.Configure(modelBuilder);
        SchedulingConfiguration.Configure(modelBuilder);
        PortalConfiguration.Configure(modelBuilder);
        ClinicalNotesConfiguration.Configure(modelBuilder);
        BillingConfiguration.Configure(modelBuilder);
        IntegrationConfiguration.Configure(modelBuilder);
    }
}

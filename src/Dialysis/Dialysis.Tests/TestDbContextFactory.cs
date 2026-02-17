using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Tests;

public static class TestDbContextFactory
{
    public static DialysisDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<DialysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new DialysisDbContext(options);
    }

    public static async Task<DialysisDbContext> CreateWithPatientAsync(Patient patient)
    {
        var db = CreateInMemory();
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return db;
    }

    public static async Task<DialysisDbContext> CreateWithObservationAsync(Domain.Aggregates.Observation observation)
    {
        var db = CreateInMemory();
        db.Observations.Add(observation);
        await db.SaveChangesAsync();
        return db;
    }
}

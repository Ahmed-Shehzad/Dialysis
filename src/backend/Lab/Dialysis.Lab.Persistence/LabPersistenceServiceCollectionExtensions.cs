using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Lab.Orders.Ports;
using Dialysis.Lab.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Lab.Persistence;

public static class LabPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LabDbContext"/> (caller supplies the provider via <paramref name="configure"/>),
    /// the unit of work, the Transponder outbox/inbox on the same context, and the lab order repository.
    /// Add migrations:
    /// <c>dotnet ef migrations add &lt;Name&gt; --project Dialysis.Lab.Persistence --startup-project Dialysis.Lab.Persistence --output-dir Migrations</c>.
    /// </summary>
    public static IServiceCollection AddLabPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configure = null)
    {
        services.AddOptions<TransponderPersistenceOptions>()
            .Configure(o => o.Schema = "transponder");

        services.AddDbContext<LabDbContext>((_, options) => configure?.Invoke(options));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LabDbContext>());
        services.AddTransponderEfOutboxAndInbox<LabDbContext>();
        services.AddScoped<ILabOrderRepository, EfLabOrderRepository>();

        return services;
    }
}

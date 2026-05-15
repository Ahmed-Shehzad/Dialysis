using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.EHR.Core.Persistence.InMemory;

/// <summary>
/// DI helpers for in-memory EHR persistence (tests / local development).
/// </summary>
public static class EhrInMemoryPersistenceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEhrInMemoryRepository<TAggregate, TId>()
        where TAggregate : AggregateRoot<TId>
        where TId : notnull
        {
            services.AddSingleton<IEhrRepository<TAggregate, TId>, InMemoryEhrRepository<TAggregate, TId>>();
            return services;
        }
    }
}

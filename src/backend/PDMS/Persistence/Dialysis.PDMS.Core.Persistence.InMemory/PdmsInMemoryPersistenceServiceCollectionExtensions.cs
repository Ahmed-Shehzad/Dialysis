using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.PDMS.Core.Persistence.InMemory;

public static class PdmsInMemoryPersistenceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPdmsInMemoryRepository<TAggregate, TId>()
        where TAggregate : AggregateRoot<TId>
        where TId : notnull
        {
            services.AddSingleton<IPdmsRepository<TAggregate, TId>, InMemoryPdmsRepository<TAggregate, TId>>();
            return services;
        }
    }
}

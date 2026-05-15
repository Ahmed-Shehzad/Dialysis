using Dialysis.BuildingBlocks.Transponder.Sagas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

public static class TransponderPersistenceSagaServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="ITransponderSagaStore"/> as <see cref="EntityFrameworkCoreTransponderSagaStore{TContext}"/> (scoped). Call after
        /// <c>AddDbContext&lt;TContext&gt;</c> (or equivalent). For production hosts that already use Transponder EF persistence, prefer this over
        /// <see cref="InMemoryTransponderSagaStore"/>.
        /// </summary>
        public IServiceCollection AddTransponderEfSagaStore<TContext>()
            where TContext : TransponderPersistenceDbContextBase
        {
            services.RemoveDescriptorsFor(typeof(ITransponderSagaStore));
            services.TryAddScoped<ITransponderSagaStore, EntityFrameworkCoreTransponderSagaStore<TContext>>();
            return services;
        }
    }
}

using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Persistence.Stores;
using Dialysis.Identity.Provisioning.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Identity.Persistence;

public static class IdentityPersistenceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIdentityPersistence(
        Action<DbContextOptionsBuilder>? configure = null)
        {
            services.AddOptions<TransponderPersistenceOptions>()
                .Configure(o => o.Schema = "identity");

            services.AddDbContext<IdentityDbContext>((sp, options) =>
            {
                configure?.Invoke(options);
                var interceptor = sp.GetService<AuditSaveChangesInterceptor>();
                if (interceptor is not null)
                    options.AddInterceptors(interceptor);

                var integrationEventOutbox = sp.GetService<IntegrationEventOutboxSaveChangesInterceptor>();
                if (integrationEventOutbox is not null)
                    options.AddInterceptors(integrationEventOutbox);
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
            services.AddTransponderEfOutboxAndInbox<IdentityDbContext>();
            services.AddModuleIntegrationEventOutbox();

            services.AddScoped<IUserAccountRepository, UserAccountRepository>();
            services.AddScoped<IRoleDefinitionRepository, RoleDefinitionRepository>();
            services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();

            return services;
        }
    }
}

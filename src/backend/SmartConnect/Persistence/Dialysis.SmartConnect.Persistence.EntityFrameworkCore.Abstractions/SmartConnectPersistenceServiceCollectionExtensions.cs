using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>Provider-neutral SmartConnect EF Core registration. Use database-specific plugin assemblies for <c>Use*</c>.</summary>
public static class SmartConnectPersistenceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="SmartConnectDbContext"/>, repositories, and <see cref="IUnitOfWork"/> for SmartConnect.
        /// </summary>
        public IServiceCollection AddSmartConnectPersistence(
            Action<DbContextOptionsBuilder> configure)
        {
            services.AddDbContext<SmartConnectDbContext>(configure);
            services.TryAddScoped<IIntegrationFlowRepository, EfIntegrationFlowRepository>();
            services.TryAddScoped<IMessageLedger, EfMessageLedger>();
            services.TryAddScoped<IMessageLedgerQuery, EfMessageLedgerQuery>();
            services.TryAddScoped<IMessageLedgerStatistics, EfMessageLedgerStatistics>();
            services.TryAddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SmartConnectDbContext>());
            services.TryAddScoped<IVariableMapStore, EfVariableMapStore>();
            services.TryAddScoped<IAuditEventStore, EfAuditEventStore>();
            services.TryAddScoped<ICodeTemplateLibraryRepository, EfCodeTemplateLibraryRepository>();
            services.TryAddScoped<IAttachmentBlobStore, EfBytesAttachmentBlobStore>();
            services.TryAddScoped<IAttachmentStore, EfAttachmentStore>();
            services.TryAddScoped<IAlertRuleRepository, EfAlertRuleRepository>();
            services.TryAddScoped<IAlertEventStore, EfAlertEventStore>();
            return services;
        }
    }
}

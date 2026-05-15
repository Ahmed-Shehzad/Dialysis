using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.CQRS;
using Dialysis.Identity.Persistence;
using Dialysis.Identity.Provisioning;
using Dialysis.Identity.Provisioning.Features.AssignRoleToUser;
using Dialysis.Identity.Provisioning.Features.DeactivateUser;
using Dialysis.Identity.Provisioning.Features.DefineRole;
using Dialysis.Identity.Provisioning.Features.ListRoles;
using Dialysis.Identity.Provisioning.Features.ListUserPermissions;
using Dialysis.Identity.Provisioning.Features.ProvisionUser;
using Dialysis.Identity.Provisioning.Features.RevokeRoleFromUser;
using Dialysis.Module.Hosting.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Identity.Composition;

public static class IdentityCompositionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Wires the Identity module's domain services, persistence, CQRS handlers, authorization
        /// pipeline behaviors, and Transponder transport bootstrap. Cross-cutting (auth, telemetry,
        /// OpenAPI, etc.) is added separately via <c>AddModuleHost</c>.
        /// </summary>
        public IServiceCollection AddIdentity(
            IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configurePersistence = null,
            bool enableOutboxRelay = false,
            Action<IServiceCollection>? configureTransponderTransport = null)
        {
            services.AddIdentityPersistence(configurePersistence);

            services.AddTransponder(_ => { });
            configureTransponderTransport?.Invoke(services);

            services.AddCqrs(c =>
            {
                c.AddFromAssembliesOf(typeof(IdentityProvisioningMarker));

                c.AddCommandBehavior<ProvisionUserCommand, Guid, AuthorizationPipelineBehavior<ProvisionUserCommand, Guid>>();
                c.AddCommandBehavior<DeactivateUserCommand, Unit, AuthorizationPipelineBehavior<DeactivateUserCommand, Unit>>();
                c.AddCommandBehavior<DefineRoleCommand, Guid, AuthorizationPipelineBehavior<DefineRoleCommand, Guid>>();
                c.AddCommandBehavior<AssignRoleToUserCommand, Unit, AuthorizationPipelineBehavior<AssignRoleToUserCommand, Unit>>();
                c.AddCommandBehavior<RevokeRoleFromUserCommand, Unit, AuthorizationPipelineBehavior<RevokeRoleFromUserCommand, Unit>>();

                c.AddQueryBehavior<ListRolesQuery, IReadOnlyList<RoleSummaryDto>, AuthorizationPipelineBehavior<ListRolesQuery, IReadOnlyList<RoleSummaryDto>>>();
                c.AddQueryBehavior<ListUserPermissionsQuery, UserPermissionsDto?, AuthorizationPipelineBehavior<ListUserPermissionsQuery, UserPermissionsDto?>>();
            });

            if (enableOutboxRelay)
                services.AddTransponderOutboxRelay<IdentityDbContext>();

            return services;
        }
    }
}

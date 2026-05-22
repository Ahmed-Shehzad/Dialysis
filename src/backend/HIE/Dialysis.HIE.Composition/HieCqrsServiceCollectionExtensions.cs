using Dialysis.CQRS;
using Dialysis.HIE.Consent.Features.GrantConsent;
using Dialysis.HIE.Consent.Features.ListConsentsForPatient;
using Dialysis.HIE.Consent.Features.RevokeConsent;
using Dialysis.HIE.Inbound.Features.ListInboundResources;
using Dialysis.HIE.Outbound.Features.ListOutboundBundles;
using Dialysis.HIE.Outbound.Features.ListPartners;
using Dialysis.HIE.Outbound.Features.RetryOutboundBundle;
using Dialysis.Module.Hosting.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIE.Composition;

internal static class HieCqrsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers authorization pipeline behaviors for the consent admin commands and queries,
        /// plus the operator-dashboard reads (outbound queue, inbound feed, partner status) and the
        /// outbound retry command. Each handler is gated by the matching <see cref="Contracts.Security.HiePermissions"/>
        /// constant.
        /// </summary>
        public IServiceCollection AddHieCqrsAuthorization() =>
            services.AddCqrs(cqrs =>
            {
                cqrs.AddCommandBehavior<GrantConsentCommand, Guid, AuthorizationPipelineBehavior<GrantConsentCommand, Guid>>();
                cqrs.AddCommandBehavior<RevokeConsentCommand, Unit, AuthorizationPipelineBehavior<RevokeConsentCommand, Unit>>();
                cqrs.AddCommandBehavior<RetryOutboundBundleCommand, Unit, AuthorizationPipelineBehavior<RetryOutboundBundleCommand, Unit>>();
                cqrs.AddQueryBehavior<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>, AuthorizationPipelineBehavior<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>>>();
                cqrs.AddQueryBehavior<ListOutboundBundlesQuery, IReadOnlyList<OutboundBundleDto>, AuthorizationPipelineBehavior<ListOutboundBundlesQuery, IReadOnlyList<OutboundBundleDto>>>();
                cqrs.AddQueryBehavior<ListInboundResourcesQuery, IReadOnlyList<InboundResourceDto>, AuthorizationPipelineBehavior<ListInboundResourcesQuery, IReadOnlyList<InboundResourceDto>>>();
                cqrs.AddQueryBehavior<ListPartnersQuery, IReadOnlyList<PartnerStatusDto>, AuthorizationPipelineBehavior<ListPartnersQuery, IReadOnlyList<PartnerStatusDto>>>();
            });
    }
}

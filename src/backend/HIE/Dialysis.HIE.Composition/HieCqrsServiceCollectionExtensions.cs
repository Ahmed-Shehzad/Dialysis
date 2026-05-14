using Dialysis.CQRS;
using Dialysis.HIE.Consent.Features.GrantConsent;
using Dialysis.HIE.Consent.Features.ListConsentsForPatient;
using Dialysis.HIE.Consent.Features.RevokeConsent;
using Dialysis.Module.Hosting.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIE.Composition;

internal static class HieCqrsServiceCollectionExtensions
{
    /// <summary>
    /// Registers authorization pipeline behaviors for the consent admin commands and queries so they enforce
    /// <c>Hie.Consent.Manage</c> before the handler runs.
    /// </summary>
    public static IServiceCollection AddHieCqrsAuthorization(this IServiceCollection services) =>
        services.AddCqrs(cqrs =>
        {
            cqrs.AddCommandBehavior<GrantConsentCommand, Guid, AuthorizationPipelineBehavior<GrantConsentCommand, Guid>>();
            cqrs.AddCommandBehavior<RevokeConsentCommand, Unit, AuthorizationPipelineBehavior<RevokeConsentCommand, Unit>>();
            cqrs.AddQueryBehavior<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>, AuthorizationPipelineBehavior<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>>>();
        });
}

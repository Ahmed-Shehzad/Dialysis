using Dialysis.CQRS;
using Dialysis.Lab.Orders;
using Dialysis.Lab.Orders.Features.GetLabOrderById;
using Dialysis.Lab.Orders.Features.ListLabOrdersByPatient;
using Dialysis.Lab.Orders.Features.PlaceLabOrder;
using Dialysis.Module.Hosting.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Lab.Composition;

/// <summary>
/// Wires the shared CQRS library into Lab: handler/validator scanning from the Orders slice plus the
/// authorization pipeline behavior for each permissioned command/query.
/// </summary>
public static class LabCqrsServiceCollectionExtensions
{
    public static IServiceCollection AddLabCqrs(this IServiceCollection services) =>
        services.AddCqrs(cqrs =>
        {
            cqrs.AddFromAssembliesOf(typeof(LabOrdersMarker));

            cqrs.AddCommandBehavior<PlaceLabOrderCommand, Guid,
                AuthorizationPipelineBehavior<PlaceLabOrderCommand, Guid>>();
            cqrs.AddQueryBehavior<GetLabOrderByIdQuery, LabOrderDto?,
                AuthorizationPipelineBehavior<GetLabOrderByIdQuery, LabOrderDto?>>();
            cqrs.AddQueryBehavior<ListLabOrdersByPatientQuery, IReadOnlyList<LabOrderSummaryDto>,
                AuthorizationPipelineBehavior<ListLabOrdersByPatientQuery, IReadOnlyList<LabOrderSummaryDto>>>();
        });
}

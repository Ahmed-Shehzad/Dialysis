using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Orders;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly Dialysis.Persistence.DialysisDbContext _db;
    private readonly IServiceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CreateOrderCommandHandler(
        Dialysis.Persistence.DialysisDbContext db,
        IServiceRequestRepository repository,
        ITenantContext tenantContext)
    {
        _db = db;
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateOrderResult> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);

        var patientExists = await _db.Patients
            .AnyAsync(p => p.TenantId == tenantId && p.LogicalId == patientId, cancellationToken);
        if (!patientExists)
            return new CreateOrderResult(null, "Patient not found.");

        var order = ServiceRequest.Create(
            tenantId,
            patientId,
            request.Code,
            request.Display,
            request.Intent,
            request.EncounterId,
            request.SessionId,
            authoredOn: DateTimeOffset.UtcNow,
            reasonText: request.ReasonText,
            requesterId: request.RequesterId,
            frequency: request.Frequency,
            category: request.Category);

        await _repository.AddAsync(order, cancellationToken);
        return new CreateOrderResult(order, null);
    }
}

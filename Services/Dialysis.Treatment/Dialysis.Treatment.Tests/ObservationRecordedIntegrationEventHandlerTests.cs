using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Domain.Events;
using Dialysis.Treatment.Application.Domain.ValueObjects;
using Dialysis.Treatment.Application.Events;
using Dialysis.Treatment.Application.Features.ObservationRecorded;

using Moq;

using Transponder.Abstractions;

namespace Dialysis.Treatment.Tests;

public class ObservationRecordedIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_PublishesIntegrationEventAsync()
    {
        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var tenant = new TenantContext { TenantId = "tenant-1" };

        var handler = new ObservationRecordedIntegrationEventHandler(publishEndpointMock.Object, tenant);
        var domainEvent = new ObservationRecordedEvent(
            Ulid.Parse("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "session-001",
            Ulid.Parse("01ARZ3NDEKTSV4RRFFQ69G5FB0"),
            new ObservationCode("158776"),
            "120",
            "mm[Hg]",
            "1.1.3.1",
            "Venous Pressure");

        await handler.HandleAsync(domainEvent);

        publishEndpointMock.Verify(
            p => p.PublishAsync(
                It.Is<ObservationRecordedIntegrationEvent>(e =>
                    e.TreatmentSessionId == domainEvent.TreatmentSessionId &&
                    e.SessionId == domainEvent.SessionId &&
                    e.ObservationId == domainEvent.ObservationId &&
                    e.Code == domainEvent.Code &&
                    e.Value == domainEvent.Value &&
                    e.Unit == domainEvent.Unit &&
                    e.SubId == domainEvent.SubId &&
                    e.ChannelName == domainEvent.ChannelName &&
                    e.TenantId == "tenant-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

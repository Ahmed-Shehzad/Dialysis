using System.Collections.Concurrent;
using BuildingBlocks.Abstractions;
using Dialysis.Contracts.Events;
using Dialysis.Messaging;
using Dialysis.Prediction;
using Dialysis.Prediction.Handlers;
using Dialysis.Prediction.Services;
using Intercessor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Dialysis.Tests;

/// <summary>
/// End-to-end tests for the ObservationCreated → Prediction → HypotensionRiskRaised → Alerting pipeline.
/// Requires Docker for the Azure Service Bus Emulator container.
/// </summary>
[Collection("ServiceBus")]
[Trait("Category", "E2E")]
public sealed class E2EPipelineTests
{
    private readonly ServiceBusFixture _fixture;

    public E2EPipelineTests(ServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ObservationCreated_publishes_HypotensionRiskRaised_when_risk_threshold_exceeded()
    {
        var captured = new ConcurrentBag<HypotensionRiskRaised>();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var connectionString = _fixture.ConnectionString;
                var address = new Uri("sb://dialysis/e2e-test");

                services.AddDialysisTransponder(address, connectionString);
                services.AddIntegrationEventConsumer<ObservationCreated>(
                    new Uri("sb://dialysis/observation-created/subscriptions/prediction-subscription"));
                services.AddIntegrationEventConsumer<HypotensionRiskRaised>(
                    new Uri("sb://dialysis/hypotension-risk-raised/subscriptions/alerting-subscription"));

                services.Configure<Prediction.Services.RiskScorerOptions>(o =>
                {
                    o.SystolicCriticalThreshold = 90;
                    o.SystolicWarningThreshold = 100;
                });
                services.AddSingleton<IVitalHistoryCache, VitalHistoryCache>();
                services.AddSingleton<IRiskScorer, EnhancedRiskScorer>();
                services.AddScoped<IIntegrationEventHandler<ObservationCreated>, ObservationCreatedIntegrationEventHandler>();
                services.AddScoped<IIntegrationEventHandler<HypotensionRiskRaised>>(_ =>
                    new HypotensionRiskCaptureHandler(captured));

                services.AddIntercessor(cfg => cfg.RegisterFromAssembly(typeof(ObservationCreatedHandler).Assembly));
            })
            .Build();

        await host.StartAsync();

        try
        {
            var publishEndpoint = host.Services.GetRequiredService<Transponder.Abstractions.IPublishEndpoint>();

            var evt = new ObservationCreated(
                Ulid.NewUlid(),
                "default",
                Dialysis.Contracts.Ids.ObservationId.Create("obs-e2e-1"),
                Dialysis.Contracts.Ids.PatientId.Create("patient-e2e"),
                Dialysis.Contracts.Ids.EncounterId.Create("encounter-e2e"),
                "8480-6",
                "85",
                DateTimeOffset.UtcNow,
                null);

            await publishEndpoint.PublishAsync(evt);

            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (captured.IsEmpty && DateTime.UtcNow < deadline)
            {
                await Task.Delay(200);
            }

            captured.ShouldNotBeEmpty("HypotensionRiskRaised should have been published by Prediction");
            var riskEvent = captured.Single();
            riskEvent.PatientId.Value.ShouldBe("patient-e2e");
            riskEvent.EncounterId.Value.ShouldBe("encounter-e2e");
            riskEvent.RiskScore.ShouldBeGreaterThanOrEqualTo(0.6);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private sealed class HypotensionRiskCaptureHandler : IIntegrationEventHandler<HypotensionRiskRaised>
    {
        private readonly ConcurrentBag<HypotensionRiskRaised> _captured;

        public HypotensionRiskCaptureHandler(ConcurrentBag<HypotensionRiskRaised> captured)
        {
            _captured = captured;
        }

        public Task HandleAsync(HypotensionRiskRaised message, CancellationToken cancellationToken = default)
        {
            _captured.Add(message);
            return Task.CompletedTask;
        }
    }
}

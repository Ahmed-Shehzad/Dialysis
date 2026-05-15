using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Api.Demo;

/// <summary>
/// Development-only seeder. Creates two well-known demo flows used by the HL7 v2 simulator and the SPA
/// Integrations page (ADT inbound + ORU lab-result inbound). Idempotent.
/// </summary>
public sealed class SmartConnectDemoSeeder(IServiceProvider services, ILogger<SmartConnectDemoSeeder> logger) : IHostedService
{
    public static readonly Guid DemoAdtFlowId = new("9d2b1a00-0000-0000-0000-00000000ad01");
    public static readonly Guid DemoOruFlowId = new("9d2b1a00-0000-0000-0000-00000000ad02");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IIntegrationFlowRepository>();

        await EnsureAsync(repo, DemoAdtFlowId, "Demo ADT^A01 inbound (HL7 v2)", "Demo flow consuming patient admit/transfer/discharge messages.", cancellationToken).ConfigureAwait(false);
        await EnsureAsync(repo, DemoOruFlowId, "Demo ORU^R01 inbound (HL7 v2)", "Demo flow consuming unsolicited lab observation results.", cancellationToken).ConfigureAwait(false);

        logger.LogInformation("SmartConnect demo seeder: ensured demo ADT + ORU flows.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureAsync(IIntegrationFlowRepository repo, Guid id, string name, string description, CancellationToken cancellationToken)
    {
        var existing = await repo.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return;

        await repo.AddAsync(new IntegrationFlow
        {
            Id = id,
            Name = name,
            Description = description,
            RuntimeState = FlowRuntimeState.Started,
            Pipeline = new IntegrationFlowPipelineDefinition(),
            Tags = ["demo"],
        }, cancellationToken).ConfigureAwait(false);
    }
}

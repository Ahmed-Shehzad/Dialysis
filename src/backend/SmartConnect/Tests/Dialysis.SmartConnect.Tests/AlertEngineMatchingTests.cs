using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AlertEngineMatchingTests
{
    [Fact]
    public async Task Rule_scoped_to_other_flow_does_not_fire()
    {
        var (sp, recordedKinds) = await BuildAsync();
        var rules = sp.GetRequiredService<IAlertRuleRepository>();
        var store = sp.GetRequiredService<IAlertEventStore>();
        var engine = sp.GetRequiredService<AlertEngine>();

        var trackedFlow = Guid.CreateVersion7();
        var otherFlow = Guid.CreateVersion7();

        await rules.UpsertAsync(new AlertRule
        {
            Id = Guid.CreateVersion7(),
            Name = "scoped",
            Enabled = true,
            EnabledFlowIds = [trackedFlow],
            ErrorPatterns = [new AlertErrorPattern { ErrorType = AlertErrorType.Any }],
            Actions = [new AlertActionSlot { Kind = "test-record" }],
        }, CancellationToken.None);

        await engine.PublishAsync(new AlertTrigger { FlowId = otherFlow, ErrorType = AlertErrorType.OutboundFailure }, CancellationToken.None);

        Assert.Empty(recordedKinds);
        Assert.Empty(await store.GetRecentAsync(50, CancellationToken.None));
    }

    [Fact]
    public async Task Regex_pattern_filters_by_error_detail()
    {
        var (sp, recordedKinds) = await BuildAsync();
        var rules = sp.GetRequiredService<IAlertRuleRepository>();
        var engine = sp.GetRequiredService<AlertEngine>();
        var flowId = Guid.CreateVersion7();

        await rules.UpsertAsync(new AlertRule
        {
            Id = Guid.CreateVersion7(),
            Name = "regex",
            Enabled = true,
            ErrorPatterns = [new AlertErrorPattern { ErrorType = AlertErrorType.Any, Regex = "(?i)timeout" }],
            Actions = [new AlertActionSlot { Kind = "test-record" }],
        }, CancellationToken.None);

        await engine.PublishAsync(new AlertTrigger { FlowId = flowId, ErrorType = AlertErrorType.OutboundFailure, ErrorDetail = "TLS handshake Timeout" }, CancellationToken.None);
        await engine.PublishAsync(new AlertTrigger { FlowId = flowId, ErrorType = AlertErrorType.OutboundFailure, ErrorDetail = "connection refused" }, CancellationToken.None);

        Assert.Single(recordedKinds);
    }

    [Fact]
    public async Task Throttle_window_suppresses_repeats()
    {
        var (sp, recordedKinds, fakeTime) = await BuildWithTimeAsync();
        var rules = sp.GetRequiredService<IAlertRuleRepository>();
        var engine = sp.GetRequiredService<AlertEngine>();
        var flowId = Guid.CreateVersion7();

        await rules.UpsertAsync(new AlertRule
        {
            Id = Guid.CreateVersion7(),
            Name = "throttle",
            Enabled = true,
            ThrottleWindow = TimeSpan.FromMinutes(5),
            ErrorPatterns = [new AlertErrorPattern { ErrorType = AlertErrorType.OutboundFailure }],
            Actions = [new AlertActionSlot { Kind = "test-record" }],
        }, CancellationToken.None);

        for (var i = 0; i < 3; i++)
        {
            await engine.PublishAsync(new AlertTrigger { FlowId = flowId, ErrorType = AlertErrorType.OutboundFailure }, CancellationToken.None);
        }
        Assert.Single(recordedKinds);

        // Advance past the window — next fire is allowed.
        fakeTime.Advance(TimeSpan.FromMinutes(6));
        await engine.PublishAsync(new AlertTrigger { FlowId = flowId, ErrorType = AlertErrorType.OutboundFailure }, CancellationToken.None);
        Assert.Equal(2, recordedKinds.Count);
    }

    [Fact]
    public async Task Rule_with_no_patterns_matches_all()
    {
        var (sp, recordedKinds) = await BuildAsync();
        var rules = sp.GetRequiredService<IAlertRuleRepository>();
        var engine = sp.GetRequiredService<AlertEngine>();

        await rules.UpsertAsync(new AlertRule
        {
            Id = Guid.CreateVersion7(),
            Name = "always",
            Enabled = true,
            Actions = [new AlertActionSlot { Kind = "test-record" }],
        }, CancellationToken.None);

        await engine.PublishAsync(new AlertTrigger { FlowId = Guid.CreateVersion7(), ErrorType = AlertErrorType.PreProcessorError }, CancellationToken.None);
        Assert.Single(recordedKinds);
    }

    private static async Task<(ServiceProvider sp, List<string> recordedKinds)> BuildAsync()
    {
        var (sp, recorded, _) = await BuildWithTimeAsync();
        return (sp, recorded);
    }

    private static Task<(ServiceProvider sp, List<string> recordedKinds, AdvanceableTimeProvider time)> BuildWithTimeAsync()
    {
        var recorded = new List<string>();
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_alert_engine_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();
        var fakeTime = new AdvanceableTimeProvider(DateTimeOffset.UtcNow);
        services.AddSingleton<TimeProvider>(fakeTime);

        var recordingProvider = new RecordingAlertActionProvider(recorded);
        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<MutableFlowPluginRegistry>().RegisterAlertActionProvider(recordingProvider);
        return Task.FromResult((sp, recorded, fakeTime));
    }

    private sealed class AdvanceableTimeProvider(DateTimeOffset startUtc) : TimeProvider
    {
        private DateTimeOffset _utcNow = startUtc;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    private sealed class RecordingAlertActionProvider(List<string> recorded) : IAlertActionProvider
    {
        public string Kind => "test-record";
        public Task<AlertActionResult> ExecuteAsync(AlertEvent evt, AlertRule rule, AlertActionSlot slot, CancellationToken ct)
        {
            recorded.Add(rule.Name);
            return Task.FromResult(AlertActionResult.Success());
        }
    }
}

using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AlertEngineMatchingTests
{
    [Fact]
    public async Task Rule_Scoped_To_Other_Flow_Does_Not_Fire_Async()
    {
        var (sp, recordedKinds) = await Build_Async();
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
    public async Task Regex_Pattern_Filters_By_Error_Detail_Async()
    {
        var (sp, recordedKinds) = await Build_Async();
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
    public async Task Throttle_Window_Suppresses_Repeats_Async()
    {
        var (sp, recordedKinds, fakeTime) = await Build_With_Time_Async();
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
    public async Task Rule_With_No_Patterns_Matches_All_Async()
    {
        var (sp, recordedKinds) = await Build_Async();
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

    private static async Task<(ServiceProvider sp, List<string> recordedKinds)> Build_Async()
    {
        var (sp, recorded, _) = await Build_With_Time_Async();
        return (sp, recorded);
    }

    private static Task<(ServiceProvider sp, List<string> recordedKinds, AdvanceableTimeProvider time)> Build_With_Time_Async()
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

    private sealed class AdvanceableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;
        public AdvanceableTimeProvider(DateTimeOffset startUtc) => _utcNow = startUtc;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    private sealed class RecordingAlertActionProvider : IAlertActionProvider
    {
        private readonly List<string> _recorded;
        public RecordingAlertActionProvider(List<string> recorded) => _recorded = recorded;
        public string Kind => "test-record";
        public Task<AlertActionResult> ExecuteAsync(AlertEvent evt, AlertRule rule, AlertActionSlot slot, CancellationToken ct)
        {
            _recorded.Add(rule.Name);
            return Task.FromResult(AlertActionResult.Success());
        }
    }
}

using Transponder.Abstractions;

namespace Transponder.Tests;

public sealed class SagaExecutionTests
{
    private sealed class TestState : ISagaStatusState
    {
        public Ulid CorrelationId { get; set; }
        public Ulid? ConversationId { get; set; }
        public int Version { get; set; }
        public SagaStatus Status { get; set; }
    }

    [Fact]
    public async Task ExecuteAsync_Completes_AllStepsAsync()
    {
        var state = new TestState { CorrelationId = Ulid.NewUlid() };
        var executed = new List<int>();

        SagaStep<TestState>[] steps =
        [
            new(
                (_, _) =>
                {
                    executed.Add(1);
                    return Task.CompletedTask;
                },
                (_, _) =>
                {
                    executed.Add(-1);
                    return Task.CompletedTask;
                }),
            new(
                (_, _) =>
                {
                    executed.Add(2);
                    return Task.CompletedTask;
                },
                (_, _) =>
                {
                    executed.Add(-2);
                    return Task.CompletedTask;
                })
        ];

        SagaStatus result = await SagaExecution.ExecuteAsync(SagaStyle.Orchestration, state, steps);

        Assert.Equal(SagaStatus.Completed, result);
        Assert.Equal(SagaStatus.Completed, state.Status);
        Assert.Equal(new[] { 1, 2 }, executed);
    }

    [Fact]
    public async Task ExecuteAsync_Compensates_WhenStepFailsAsync()
    {
        var state = new TestState { CorrelationId = Ulid.NewUlid() };
        var executed = new List<int>();

        SagaStep<TestState>[] steps =
        [
            new(
                (_, _) =>
                {
                    executed.Add(1);
                    return Task.CompletedTask;
                },
                (_, _) =>
                {
                    executed.Add(-1);
                    return Task.CompletedTask;
                }),
            new(
                (_, _) =>
                {
                    executed.Add(2);
                    throw new InvalidOperationException("boom");
                },
                (_, _) =>
                {
                    executed.Add(-2);
                    return Task.CompletedTask;
                })
        ];

        SagaStatus result = await SagaExecution.ExecuteAsync(SagaStyle.Orchestration, state, steps);

        Assert.Equal(SagaStatus.Compensated, result);
        Assert.Equal(SagaStatus.Compensated, state.Status);
        Assert.Equal(new[] { 1, 2, -1 }, executed);
    }
}

using Transponder.Persistence;
using Transponder.Persistence.Abstractions;

namespace Transponder.Tests;

public sealed class SagaRepositoryConcurrencyTests
{
    [Fact]
    public async Task SaveAsync_Should_Return_False_On_Concurrency_ConflictAsync()
    {
        // Arrange
        var repository = new InMemorySagaRepository<TestSagaState>();
        var correlationId = Ulid.NewUlid();

        var state1 = new TestSagaState
        {
            CorrelationId = correlationId,
            Version = 0
        };

        var state2 = new TestSagaState
        {
            CorrelationId = correlationId,
            Version = 0
        };

        // Act - Save first state
        bool saved1 = await repository.SaveAsync(state1);
        Assert.True(saved1);
        Assert.Equal(1, state1.Version);

        // Get the updated state
        TestSagaState? retrieved = await repository.GetAsync(correlationId);
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved!.Version);

        // Try to save state2 with old version (should fail)
        state2.Version = 0; // Old version
        bool saved2 = await repository.SaveAsync(state2);

        // Assert
        Assert.False(saved2);
        Assert.Equal(0, state2.Version); // Version should not be incremented on failure
    }

    [Fact]
    public async Task SaveAsync_Should_Increment_Version_On_SuccessAsync()
    {
        // Arrange
        var repository = new InMemorySagaRepository<TestSagaState>();
        var correlationId = Ulid.NewUlid();

        var state = new TestSagaState
        {
            CorrelationId = correlationId,
            Version = 0
        };

        // Act
        bool saved = await repository.SaveAsync(state);

        // Assert
        Assert.True(saved);
        Assert.Equal(1, state.Version);

        // Save again with correct version
        bool saved2 = await repository.SaveAsync(state);
        Assert.True(saved2);
        Assert.Equal(2, state.Version);
    }

    [Fact]
    public async Task SaveAsync_Should_Set_Version_To_1_For_New_StateAsync()
    {
        // Arrange
        var repository = new InMemorySagaRepository<TestSagaState>();
        var correlationId = Ulid.NewUlid();

        var state = new TestSagaState
        {
            CorrelationId = correlationId,
            Version = 0
        };

        // Act
        bool saved = await repository.SaveAsync(state);

        // Assert
        Assert.True(saved);
        Assert.Equal(1, state.Version);

        TestSagaState? retrieved = await repository.GetAsync(correlationId);
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved!.Version);
    }

    private sealed class TestSagaState : ISagaState
    {
        public Ulid CorrelationId { get; set; }
        public Ulid? ConversationId { get; set; }
        public int Version { get; set; }
    }
}

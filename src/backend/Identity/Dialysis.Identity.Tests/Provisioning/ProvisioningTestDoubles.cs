using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Tests.Provisioning;

/// <summary>
/// Hand-rolled in-memory ports for the provisioning handlers. The handlers' business rules
/// (idempotency, deactivated-user gating, permission snapshots) don't need Postgres — the
/// repository contract is small enough that dictionaries are an honest implementation.
/// </summary>
internal sealed class InMemoryUserAccountRepository : IUserAccountRepository
{
    private readonly Dictionary<Guid, UserAccount> _users = [];

    public IReadOnlyCollection<UserAccount> All => _users.Values;

    public Task<UserAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<UserAccount?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Subject == subject));

    public void Add(UserAccount user) => _users[user.Id] = user;

    public void Update(UserAccount user) => _users[user.Id] = user;
}

/// <summary>In-memory <see cref="IRoleDefinitionRepository"/>.</summary>
internal sealed class InMemoryRoleDefinitionRepository : IRoleDefinitionRepository
{
    private readonly Dictionary<Guid, RoleDefinition> _roles = [];

    public IReadOnlyCollection<RoleDefinition> All => _roles.Values;

    public Task<RoleDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.GetValueOrDefault(id));

    public Task<RoleDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.Values.FirstOrDefault(r => r.Code == code));

    public Task<IReadOnlyList<RoleDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoleDefinition>>([.. _roles.Values]);

    public Task<IReadOnlyList<RoleDefinition>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoleDefinition>>([.. _roles.Values.Where(r => ids.Contains(r.Id))]);

    public void Add(RoleDefinition role) => _roles[role.Id] = role;
}

/// <summary>In-memory <see cref="IRoleAssignmentRepository"/>.</summary>
internal sealed class InMemoryRoleAssignmentRepository : IRoleAssignmentRepository
{
    private readonly List<RoleAssignment> _assignments = [];

    public IReadOnlyList<RoleAssignment> All => _assignments;

    public Task<RoleAssignment?> FindAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignments.FirstOrDefault(a => a.UserId == userId && a.RoleId == roleId));

    public Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoleAssignment>>([.. _assignments.Where(a => a.UserId == userId)]);

    public void Add(RoleAssignment assignment) => _assignments.Add(assignment);

    public void Remove(RoleAssignment assignment) => _assignments.Remove(assignment);
}

/// <summary>Records every envelope enqueued to the transactional outbox.</summary>
internal sealed class RecordingTransponderOutbox : ITransponderOutbox
{
    public List<TransponderOutboxEnvelope> Enqueued { get; } = [];

    public Task EnqueueAsync(TransponderOutboxEnvelope envelope, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(envelope);
        return Task.CompletedTask;
    }
}

/// <summary>Records every message published straight to the bus (no outbox leg).</summary>
internal sealed class RecordingTransponderBus : ITransponderBus
{
    public List<object> Published { get; } = [];

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>System.Text.Json stand-in for the Transponder serializer.</summary>
internal sealed class JsonTestMessageSerializer : IMessageSerializer
{
    public ReadOnlyMemory<byte> Serialize<T>(T message)
        where T : class =>
        JsonSerializer.SerializeToUtf8Bytes(message);

    public ReadOnlyMemory<byte> Serialize(Type messageType, object message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, messageType);

    public object? Deserialize(Type messageType, ReadOnlyMemory<byte> payload) =>
        JsonSerializer.Deserialize(payload.Span, messageType);
}

/// <summary>Counts commits so tests can assert a handler did (or did not) reach its save.</summary>
internal sealed class CountingUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

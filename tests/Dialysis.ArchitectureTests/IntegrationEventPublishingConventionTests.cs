using System.Reflection;
using Shouldly;
using Xunit;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// Integration events must be raised by aggregates (<c>AggregateRoot.RaiseIntegrationEvent</c>) and
/// reach the broker through the transactional outbox — drained either by the
/// <c>IntegrationEventOutboxSaveChangesInterceptor</c> on SaveChanges or via an explicit
/// <c>ITransponderOutbox.EnqueueAsync</c> in the same unit of work. Publishing straight to
/// <c>ITransponderBus</c> from a command handler or consumer bypasses the outbox: the event can
/// escape a rolled-back transaction or be lost after a committed one. This gate forbids the bus as
/// a constructor dependency of command handlers and consumers in the DDD modules.
///
/// SmartConnect is exempt by design (channel/flow integration engine — publishing IS its delivery
/// step, retried by the message ledger); BFF event-push consumers are read-side notifications and
/// live outside these assemblies.
/// </summary>
public sealed class IntegrationEventPublishingConventionTests
{
    private static readonly string[] _dddModulePrefixes =
    [
        "Dialysis.HIS.",
        "Dialysis.EHR.",
        "Dialysis.PDMS.",
        "Dialysis.HIE.",
        "Dialysis.Lab.",
        "Dialysis.Identity.",
        "Dialysis.PatientPortal.",
    ];

    /// <summary>
    /// Documented exceptions. Each entry must justify itself here:
    /// - BillingExportJobQueuedConsumer: keeps the bus solely for the failure path — the unit of
    ///   work just rolled back, so an outbox row would roll back with it; the failed-batch signal
    ///   must escape the dead transaction for HIS to move the export job out of Queued.
    /// </summary>
    private static readonly string[] _allowedBusDependents =
    [
        "Dialysis.EHR.Billing.Consumers.BillingExportJobQueuedConsumer",
    ];

    [Fact]
    public void Command_handlers_and_consumers_must_not_inject_the_transponder_bus()
    {
        var candidates = LoadCandidates();

        var offenders = candidates
            .Where(t => !_allowedBusDependents.Contains(t.FullName))
            .Where(t => InjectsParameterOfType(t, "Dialysis.BuildingBlocks.Transponder.ITransponderBus"))
            .Select(t => t.FullName!)
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty(
            "Command handlers/consumers must not publish integration events directly via ITransponderBus. "
            + "Raise the event on the aggregate (RaiseIntegrationEvent — the SaveChanges interceptor drains it "
            + "to the outbox) or enqueue it via ITransponderOutbox in the same unit of work. Offenders:\n  - "
            + string.Join("\n  - ", offenders));
    }

    [Fact]
    public void Command_handlers_must_not_enqueue_to_the_outbox_directly()
    {
        var offenders = LoadCandidates()
            .Where(t => ImplementsOpenGeneric(t, "Dialysis.CQRS.Commands.ICommandHandler`"))
            .Where(t => InjectsParameterOfType(t, "Dialysis.BuildingBlocks.Transponder.ITransponderOutbox"))
            .Select(t => t.FullName!)
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty(
            "Command handlers must not hand-drain events into ITransponderOutbox — that is the interceptor's "
            + "job. Raise the integration event on the aggregate (RaiseIntegrationEvent) and let the "
            + "IntegrationEventOutboxSaveChangesInterceptor persist it in the same transaction. "
            + "(Consumers emitting process signals with no natural aggregate may still use the outbox.) "
            + "Offenders:\n  - "
            + string.Join("\n  - ", offenders));
    }

    private static List<Type> LoadCandidates()
    {
        var candidates = ModuleAssemblyLoader.LoadAll()
            .Where(a => a.GetName().Name is { } n
                && _dddModulePrefixes.Any(p => n.StartsWith(p, StringComparison.Ordinal))
                && !n.Contains(".Bff", StringComparison.Ordinal))
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && (ImplementsOpenGeneric(t, "Dialysis.CQRS.Commands.ICommandHandler`")
                    || ImplementsOpenGeneric(t, "Dialysis.BuildingBlocks.Transponder.IConsumer`")))
            .ToList();

        candidates.ShouldNotBeEmpty("Expected at least one command handler or consumer in the DDD modules.");
        return candidates;
    }

    private static bool InjectsParameterOfType(Type t, string parameterTypeFullName) =>
        t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(c => c.GetParameters()
                .Any(p => p.ParameterType.FullName == parameterTypeFullName));

    private static bool ImplementsOpenGeneric(Type t, string interfaceFullNamePrefix) =>
        t.GetInterfaces().Any(i =>
            i.IsGenericType
            && i.GetGenericTypeDefinition().FullName is { } name
            && name.StartsWith(interfaceFullNamePrefix, StringComparison.Ordinal));

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        {
            return a.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}

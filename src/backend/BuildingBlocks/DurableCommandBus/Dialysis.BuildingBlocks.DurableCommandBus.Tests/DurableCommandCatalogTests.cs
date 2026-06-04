using Dialysis.CQRS.Commands;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.DurableCommandBus.Tests;

/// <summary>
/// Locks down the catalog's two responsibilities: (1) translate the wire <c>CommandTypeKey</c>
/// to a registered handler, and (2) reject anything not on the allowlist — the only gate
/// between a poisoned envelope and arbitrary handler invocation.
/// </summary>
public sealed class DurableCommandCatalogTests
{
    public sealed record SampleCommand : ICommand<int>
    {
        public SampleCommand(string Note) => this.Note = Note;
        public string Note { get; init; }
        public void Deconstruct(out string note) => note = this.Note;
    }

    public sealed record UnregisteredCommand : ICommand<int>;

    [Fact]
    public void Registered_Command_Is_Resolvable_By_Type_Key_And_By_Clr_Type()
    {
        var builder = new DurableCommandsBuilder("test");
        builder.RegisterCommand<SampleCommand, int>(requiredPermission: "test.sample");

        var catalog = new DurableCommandCatalog(builder.Registrations);

        catalog.TryGet(typeof(SampleCommand).FullName!, out var byKey).ShouldBeTrue();
        byKey!.CommandType.ShouldBe(typeof(SampleCommand));
        byKey.ResultType.ShouldBe(typeof(int));
        byKey.RequiredPermission.ShouldBe("test.sample");

        catalog.TryGetForType(typeof(SampleCommand), out var byType).ShouldBeTrue();
        byType!.CommandTypeKey.ShouldBe(typeof(SampleCommand).FullName);
    }

    [Fact]
    public void Unregistered_Type_Is_Rejected_By_Both_Lookups()
    {
        var builder = new DurableCommandsBuilder("test");
        builder.RegisterCommand<SampleCommand, int>();

        var catalog = new DurableCommandCatalog(builder.Registrations);

        catalog.TryGet("Some.Foreign.CommandType, Some.Assembly", out var byKey).ShouldBeFalse();
        byKey.ShouldBeNull();

        catalog.TryGetForType(typeof(UnregisteredCommand), out var byType).ShouldBeFalse();
        byType.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_Closure_Restores_The_Concrete_Command_Instance()
    {
        var builder = new DurableCommandsBuilder("test");
        builder.RegisterCommand<SampleCommand, int>();

        var catalog = new DurableCommandCatalog(builder.Registrations);
        catalog.TryGet(typeof(SampleCommand).FullName!, out var registration).ShouldBeTrue();

        var restored = registration!.Deserialize("{\"note\":\"hello\"}");

        restored.ShouldBeOfType<SampleCommand>();
        ((SampleCommand)restored).Note.ShouldBe("hello");
    }
}

using Dialysis.BuildingBlocks.Transponder;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace Dialysis.Module.Bff.Events.Tests;

/// <summary>
/// Coverage for <see cref="ModuleBffEventsExtensions.AddModuleBffEvents"/>: SignalR + the notifier
/// are always registered, and the RabbitMQ transport is opt-in on the connection URI (so dev/tests
/// run the in-process bus and never require a broker).
/// </summary>
public sealed class ModuleBffEventsRegistrationTests
{
    [Fact]
    public void Registers_The_Bff_Notifier_And_The_In_Process_Bus_When_No_Connection_Uri()
    {
        var builder = WebApplication.CreateBuilder([]);
        builder.AddModuleBffEvents();

        var busDescriptors = builder.Services
            .Where(d => d.ServiceType == typeof(ITransponderBus))
            .ToList();

        builder.Services.ShouldContain(d => d.ServiceType == typeof(IBffNotifier));
        busDescriptors.ShouldContain(d => d.ImplementationType != null && d.ImplementationType.Name == "TransponderBus");
        busDescriptors.ShouldNotContain(d => d.ImplementationType != null && d.ImplementationType.Name == "RabbitMqTransponderBus");
    }

    [Fact]
    public void Attaches_The_Rabbit_Mq_Bus_When_A_Connection_Uri_Is_Configured()
    {
        var builder = WebApplication.CreateBuilder([]);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bff:Module:Slug"] = "ehr",
            ["Bff:Events:RabbitMq:ConnectionUri"] = "amqp://guest:guest@localhost:5672/",
        });

        builder.AddModuleBffEvents();

        builder.Services
            .Where(d => d.ServiceType == typeof(ITransponderBus))
            .ShouldContain(d => d.ImplementationType != null && d.ImplementationType.Name == "RabbitMqTransponderBus");
    }
}

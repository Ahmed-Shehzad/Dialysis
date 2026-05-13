using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AlertRuleRepositoryTests
{
    [Fact]
    public async Task Upsert_then_get_round_trips_all_fields()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<IAlertRuleRepository>();
        var id = Guid.CreateVersion7();
        var flowId = Guid.CreateVersion7();

        var rule = new AlertRule
        {
            Id = id,
            Name = "HL7 parse errors",
            Enabled = true,
            Description = "Notify when parsing fails",
            EnabledFlowIds = [flowId],
            ErrorPatterns =
            [
                new AlertErrorPattern { ErrorType = AlertErrorType.TransformError, Regex = "parse" },
            ],
            Actions =
            [
                new AlertActionSlot { Kind = "email", PropertiesJson = """{"host":"x","from":"a","to":"b"}""" },
            ],
            ThrottleWindow = TimeSpan.FromSeconds(120),
            LastModifiedUtc = DateTimeOffset.UtcNow,
        };
        await repo.UpsertAsync(rule, CancellationToken.None);

        var fetched = await repo.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("HL7 parse errors", fetched!.Name);
        Assert.True(fetched.Enabled);
        Assert.NotNull(fetched.EnabledFlowIds);
        Assert.Single(fetched.EnabledFlowIds!);
        Assert.Equal(flowId, fetched.EnabledFlowIds![0]);
        Assert.Single(fetched.ErrorPatterns);
        Assert.Equal(AlertErrorType.TransformError, fetched.ErrorPatterns[0].ErrorType);
        Assert.Equal("parse", fetched.ErrorPatterns[0].Regex);
        Assert.Single(fetched.Actions);
        Assert.Equal("email", fetched.Actions[0].Kind);
        Assert.Equal(TimeSpan.FromSeconds(120), fetched.ThrottleWindow);
    }

    [Fact]
    public async Task GetEnabled_filters_disabled()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<IAlertRuleRepository>();

        await repo.UpsertAsync(new AlertRule { Id = Guid.CreateVersion7(), Name = "on", Enabled = true }, CancellationToken.None);
        await repo.UpsertAsync(new AlertRule { Id = Guid.CreateVersion7(), Name = "off", Enabled = false }, CancellationToken.None);

        var enabled = await repo.GetEnabledAsync(CancellationToken.None);
        Assert.Single(enabled);
        Assert.Equal("on", enabled[0].Name);
    }

    [Fact]
    public async Task Empty_enabled_flow_ids_round_trips_as_null_for_all_flows()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<IAlertRuleRepository>();
        var id = Guid.CreateVersion7();

        await repo.UpsertAsync(new AlertRule { Id = id, Name = "all", Enabled = true }, CancellationToken.None);
        var fetched = await repo.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Null(fetched!.EnabledFlowIds);
    }

    [Fact]
    public async Task Delete_removes_the_row()
    {
        await using var sp = BuildServices();
        var repo = sp.GetRequiredService<IAlertRuleRepository>();
        var id = Guid.CreateVersion7();
        await repo.UpsertAsync(new AlertRule { Id = id, Name = "x", Enabled = true }, CancellationToken.None);
        await repo.DeleteAsync(id, CancellationToken.None);
        Assert.Null(await repo.GetByIdAsync(id, CancellationToken.None));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_alerts_{Guid.NewGuid():N}");
        return services.BuildServiceProvider();
    }
}

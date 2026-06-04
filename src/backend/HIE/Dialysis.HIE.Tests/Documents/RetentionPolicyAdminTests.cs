using Dialysis.CQRS;
using Dialysis.HIE.Documents.Features.Retention;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

/// <summary>
/// Guards that the <c>Dialysis.HIE.Documents</c> slice is registered in the host's
/// <c>HandlerAssemblies</c>. When the assembly was missing, the retention-policy command/query
/// handlers never resolved, so the retention admin endpoints returned HTTP 500 — see the e2e
/// <c>document-retention</c> spec.
/// </summary>
public sealed class RetentionPolicyAdminTests
{
    [Fact]
    public async Task Upsert_Then_List_Retention_Policy_Round_Trip_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var cqrs = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var id = await cqrs.SendCommandAsync<UpsertRetentionPolicyCommand, Guid>(
            new UpsertRetentionPolicyCommand("DischargeLetter", 365, "operator"));

        var rows = await cqrs.SendQueryAsync<ListRetentionPoliciesQuery, IReadOnlyList<RetentionPolicyRow>>(
            new ListRetentionPoliciesQuery());

        rows.ShouldContain(r => r.Id == id && r.Kind == "DischargeLetter" && r.RetentionDays == 365);
    }
}

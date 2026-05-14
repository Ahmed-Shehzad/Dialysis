using Dialysis.CQRS;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.Security.Features.RegisterLocalUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class SecurityFlowTests(HisApiWebApplicationFactory factory)
{
    [Fact]
    public async Task RegisterLocalUser_persists_and_returns_id()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var login = $"user-{Guid.NewGuid():N}".ToLowerInvariant()[..20];
        var id = await gateway.SendCommandAsync<RegisterLocalUserCommand, Guid>(
            new RegisterLocalUserCommand(login, "Test User"),
            CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);

        var persisted = await db.LocalUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, CancellationToken.None);
        persisted.ShouldNotBeNull();
        persisted.DisplayName.ShouldBe("Test User");
        persisted.IsActive.ShouldBeTrue();
    }
}

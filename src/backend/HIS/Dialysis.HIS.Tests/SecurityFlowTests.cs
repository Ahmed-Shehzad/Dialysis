using Dialysis.CQRS;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.Security.Features.RegisterLocalUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class SecurityFlowTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public SecurityFlowTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Registerlocaluser_Persists_And_Returns_Id_Async()
    {
        using var scope = _factory.Services.CreateScope();
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

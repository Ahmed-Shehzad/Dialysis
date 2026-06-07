using System.Security.Claims;
using Dialysis.EHR.Api.Security;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Module.Hosting.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Security;

/// <summary>
/// The portal self-access gate: a caller may act as their own patient, and — only when the dev
/// impersonation flag is on AND they hold a clinician permission — as any patient. The flag is off in
/// production, so impersonation is never reachable there.
/// </summary>
public sealed class EhrPortalAccessTests
{
    private static readonly Guid _patient = Guid.Parse("6f4d069d-ed97-4943-9aed-8644be415496");

    private static EhrPortalAccess Build(bool impersonationFlag, IReadOnlyCollection<string> permissions)
    {
        var authOptions = Options.Create(new ModuleAuthenticationOptions { Authority = "https://idp.example/realms/dialysis" });
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ehr:Portal:AllowStaffImpersonation"] = impersonationFlag ? "true" : "false",
            })
            .Build();
        return new EhrPortalAccess(authOptions, config, new StubCurrentUser(permissions));
    }

    private static ClaimsPrincipal Caller(string? hisPatientId) =>
        new(new ClaimsIdentity(
            hisPatientId is null ? [] : [new Claim("his_patient_id", hisPatientId)],
            authenticationType: "test"));

    [Fact]
    public void Patient_Can_Act_As_Self()
    {
        var access = Build(impersonationFlag: false, permissions: ["ehr.portal.read"]);
        access.CanActAs(Caller(_patient.ToString()), _patient).ShouldBeTrue();
    }

    [Fact]
    public void Patient_Cannot_Act_As_Another_Patient()
    {
        var access = Build(impersonationFlag: false, permissions: ["ehr.portal.read"]);
        access.CanActAs(Caller(Guid.NewGuid().ToString()), _patient).ShouldBeFalse();
    }

    [Fact]
    public void Staff_With_Flag_Off_Cannot_Impersonate()
    {
        var access = Build(impersonationFlag: false, permissions: [EhrPermissions.ChartRead]);
        access.CanActAs(Caller(hisPatientId: null), _patient).ShouldBeFalse();
    }

    [Fact]
    public void Staff_With_Flag_On_Can_Impersonate()
    {
        var access = Build(impersonationFlag: true, permissions: [EhrPermissions.ChartRead]);
        access.CanActAs(Caller(hisPatientId: null), _patient).ShouldBeTrue();
    }

    [Fact]
    public void Non_Staff_With_Flag_On_Cannot_Impersonate()
    {
        // A real portal patient holds only ehr.portal.* permissions, never the clinician ChartRead.
        var access = Build(impersonationFlag: true, permissions: ["ehr.portal.read"]);
        access.CanActAs(Caller(hisPatientId: null), _patient).ShouldBeFalse();
    }

    private sealed class StubCurrentUser(IReadOnlyCollection<string> permissions) : ICurrentUser
    {
        public string? UserId => "test-user";
        public IReadOnlyCollection<string> Permissions { get; } = permissions;
    }
}

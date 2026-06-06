using System.Security.Claims;
using Dialysis.EHR.Api.Security;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Security;

public sealed class EhrPatientAccessTests
{
    private static readonly Guid _patient = Guid.NewGuid();
    private static readonly Guid _other = Guid.NewGuid();

    [Fact]
    public void Is_Self_When_His_Patient_Id_Claim_Matches()
    {
        var user = Principal(("his_patient_id", _patient.ToString()));
        EhrPatientAccess.IsSelf(user, _patient, authorityConfigured: true).ShouldBeTrue();
        EhrPatientAccess.IsSelf(user, _other, authorityConfigured: true).ShouldBeFalse();
    }

    [Fact]
    public void Falls_Back_To_Sub_When_No_His_Patient_Id()
    {
        var user = Principal(("sub", _patient.ToString()));
        EhrPatientAccess.IsSelf(user, _patient, authorityConfigured: true).ShouldBeTrue();
    }

    [Fact]
    public void Denies_When_No_Patient_Claim_And_Authority_Configured()
    {
        var user = Principal(("name", "someone"));
        EhrPatientAccess.IsSelf(user, _patient, authorityConfigured: true).ShouldBeFalse();
    }

    [Fact]
    public void Dev_Bypass_When_No_Authority_Configured()
    {
        var user = Principal(("name", "someone"));
        EhrPatientAccess.IsSelf(user, _patient, authorityConfigured: false).ShouldBeTrue();
    }

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));
}

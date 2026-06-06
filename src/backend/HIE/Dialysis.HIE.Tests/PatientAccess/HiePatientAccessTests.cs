using System.Security.Claims;
using Dialysis.HIE.Api.Controllers;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.PatientAccess;

public sealed class HiePatientAccessTests
{
    [Fact]
    public void Is_Self_When_His_Patient_Id_Claim_Matches()
    {
        var user = Principal(("his_patient_id", "patient-1"));
        HiePatientAccess.IsSelf(user, "patient-1").ShouldBeTrue();
        HiePatientAccess.IsSelf(user, "patient-2").ShouldBeFalse();
    }

    [Fact]
    public void Falls_Back_To_Sub_When_No_His_Patient_Id()
    {
        var user = Principal(("sub", "patient-9"));
        HiePatientAccess.PatientId(user).ShouldBe("patient-9");
        HiePatientAccess.IsSelf(user, "patient-9").ShouldBeTrue();
    }

    [Fact]
    public void Denies_When_No_Patient_Claim()
    {
        var user = Principal(("name", "someone"));
        HiePatientAccess.PatientId(user).ShouldBeNull();
        HiePatientAccess.IsSelf(user, "patient-1").ShouldBeFalse();
    }

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));
}

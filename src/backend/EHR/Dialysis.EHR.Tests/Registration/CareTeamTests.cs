using Dialysis.EHR.Registration.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Registration;

public sealed class CareTeamTests
{
    private static CareTeam NewTeam() =>
        CareTeam.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

    [Fact]
    public void Add_Members_With_Roles()
    {
        var team = NewTeam();
        team.AddMember(Guid.NewGuid(), Guid.NewGuid(), CareTeamRole.PrimaryNephrologist, isPrimary: true);
        team.AddMember(Guid.NewGuid(), Guid.NewGuid(), CareTeamRole.DialysisNurse, isPrimary: false);

        team.Members.Count.ShouldBe(2);
        team.Members.Count(m => m.IsPrimary).ShouldBe(1);
    }

    [Fact]
    public void Cannot_Add_The_Same_Provider_Twice()
    {
        var team = NewTeam();
        var provider = Guid.NewGuid();
        team.AddMember(Guid.NewGuid(), provider, CareTeamRole.AttendingPhysician, isPrimary: false);
        Should.Throw<InvalidOperationException>(() =>
            team.AddMember(Guid.NewGuid(), provider, CareTeamRole.Other, isPrimary: false));
    }

    [Fact]
    public void At_Most_One_Primary_After_Adding_A_Second_Primary()
    {
        var team = NewTeam();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        team.AddMember(Guid.NewGuid(), first, CareTeamRole.PrimaryNephrologist, isPrimary: true);
        team.AddMember(Guid.NewGuid(), second, CareTeamRole.AttendingPhysician, isPrimary: true);

        team.Members.Count(m => m.IsPrimary).ShouldBe(1);
        team.Members.Single(m => m.IsPrimary).ProviderId.ShouldBe(second);
    }

    [Fact]
    public void Set_Primary_Demotes_The_Current_Primary()
    {
        var team = NewTeam();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        team.AddMember(Guid.NewGuid(), a, CareTeamRole.PrimaryNephrologist, isPrimary: true);
        team.AddMember(Guid.NewGuid(), b, CareTeamRole.AttendingPhysician, isPrimary: false);

        team.SetPrimary(b);

        team.Members.Single(m => m.IsPrimary).ProviderId.ShouldBe(b);
    }

    [Fact]
    public void Remove_Member_Drops_Them_From_The_Roster()
    {
        var team = NewTeam();
        var provider = Guid.NewGuid();
        team.AddMember(Guid.NewGuid(), provider, CareTeamRole.Dietitian, isPrimary: false);
        team.RemoveMember(provider);
        team.Members.ShouldBeEmpty();
    }
}

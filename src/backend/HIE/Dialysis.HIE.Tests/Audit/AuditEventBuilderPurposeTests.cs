using Dialysis.BuildingBlocks.Fhir.Audit;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Audit;

public sealed class AuditEventBuilderPurposeTests
{
    [Fact]
    public void Read_Records_Purpose_Of_Event_When_Provided()
    {
        var auditEvent = AuditEventBuilder.Read("Patient", "p1", "agent-1", "hie", purposeOfUse: "Treatment");

        auditEvent.PurposeOfEvent.ShouldHaveSingleItem()
            .Coding.ShouldContain(c =>
                c.System == AuditEventBuilder.TefcaPurposeOfUseSystem && c.Code == "Treatment");
        // The acting agent also carries the purpose-of-use, per the AuditEvent profile.
        auditEvent.Agent.ShouldHaveSingleItem()
            .PurposeOfUse.ShouldContain(cc => cc.Coding.Exists(c => c.Code == "Treatment"));
    }

    [Fact]
    public void Read_Omits_Purpose_Of_Event_When_Not_Provided()
    {
        var auditEvent = AuditEventBuilder.Read("Patient", "p1", "agent-1", "hie");

        auditEvent.PurposeOfEvent.ShouldBeEmpty();
        auditEvent.Agent.ShouldHaveSingleItem().PurposeOfUse.ShouldBeEmpty();
    }
}

using Dialysis.EHR.Billing.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// State-machine tests for the new Claim acknowledgement surface. We only assert the
/// transitions the upstream consumers depend on — 999 acceptance moves
/// Submitted → Acknowledged, 277CA rejection moves whatever-state → Denied, etc.
/// </summary>
public sealed class ClaimAcknowledgementTests
{
    private static Claim SubmittedClaim()
    {
        var patientId = Guid.NewGuid();
        var charge = Charge.Capture(
            Guid.NewGuid(), patientId, Guid.NewGuid(),
            "90935", ["N18.6"], new Money(250m, "USD"));
        var claim = Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01", "EDI837P", [charge]);
        claim.Submit("CTRL-1", DateTime.UtcNow);
        return claim;
    }

    [Fact]
    public void Accepted_999_Moves_Submitted_To_Acknowledged()
    {
        var claim = SubmittedClaim();

        claim.RecordAcknowledgement(new ClaimAcknowledgement(
            Guid.NewGuid(), ClaimAckKind.FunctionalAck999, ClaimAckVerdict.Accepted,
            payerClaimControlNumber: null, reasonCodes: [], receivedAtUtc: DateTime.UtcNow));

        claim.Status.ShouldBe(ClaimStatus.Acknowledged);
        claim.AcknowledgedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Rejected_999_Moves_Submitted_To_Denied()
    {
        var claim = SubmittedClaim();

        claim.RecordAcknowledgement(new ClaimAcknowledgement(
            Guid.NewGuid(), ClaimAckKind.FunctionalAck999, ClaimAckVerdict.Rejected,
            payerClaimControlNumber: null, reasonCodes: ["IK3*CLM*5*0*8"], receivedAtUtc: DateTime.UtcNow));

        claim.Status.ShouldBe(ClaimStatus.Denied);
    }

    [Fact]
    public void Accepted_Claim_Ack_Captures_The_Payer_Claim_Control_Number()
    {
        var claim = SubmittedClaim();

        claim.RecordAcknowledgement(new ClaimAcknowledgement(
            Guid.NewGuid(), ClaimAckKind.ClaimAck277Ca, ClaimAckVerdict.Accepted,
            payerClaimControlNumber: "PAY-9876", reasonCodes: ["A2/20"], receivedAtUtc: DateTime.UtcNow));

        claim.PayerClaimControlNumber.ShouldBe("PAY-9876");
        claim.Status.ShouldBe(ClaimStatus.Acknowledged);
    }

    [Fact]
    public void Acknowledgement_History_Preserves_Every_Recorded_Ack()
    {
        var claim = SubmittedClaim();
        claim.RecordAcknowledgement(new ClaimAcknowledgement(
            Guid.NewGuid(), ClaimAckKind.FunctionalAck999, ClaimAckVerdict.Accepted,
            null, [], DateTime.UtcNow));
        claim.RecordAcknowledgement(new ClaimAcknowledgement(
            Guid.NewGuid(), ClaimAckKind.ClaimAck277Ca, ClaimAckVerdict.Accepted,
            "PAY-9876", ["A2/20"], DateTime.UtcNow.AddMinutes(5)));

        claim.Acknowledgements.Count.ShouldBe(2);
        claim.Acknowledgements.Select(a => a.Kind)
            .ShouldBe([ClaimAckKind.FunctionalAck999, ClaimAckKind.ClaimAck277Ca]);
    }
}

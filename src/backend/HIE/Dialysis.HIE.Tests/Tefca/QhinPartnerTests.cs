using Dialysis.HIE.Tefca.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Tefca;

public sealed class QhinPartnerTests
{
    [Fact]
    public void New_Partner_Starts_In_Onboarding()
    {
        var partner = Make_Partner();
        partner.Status.ShouldBe(QhinPartnerStatus.Onboarding);
        partner.TrustAnchors.ShouldBeEmpty();
        partner.MtlsCertThumbprint.ShouldBeNull();
    }

    [Fact]
    public void Invalid_Fhir_Base_Url_Throws()
    {
        Should.Throw<ArgumentException>(() => new QhinPartner(
            Guid.CreateVersion7(), "Test", "not-a-url", "https://ias.example/oauth",
            DateTime.UtcNow, "dpo"));
    }

    [Fact]
    public void Cannot_Activate_Without_Trust_Anchor_Or_Mtls()
    {
        var partner = Make_Partner();
        Should.Throw<InvalidOperationException>(() =>
            partner.TransitionStatus(QhinPartnerStatus.Active, DateTime.UtcNow, "dpo"));
    }

    [Fact]
    public void Can_Activate_After_Anchor_And_Mtls()
    {
        var partner = Make_Partner();
        partner.AttachTrustAnchor(Make_Anchor(partner.Id, "AA"));
        partner.RotateMtls("inmem://blobs/x", "MTLS-AA", DateTime.UtcNow, "dpo");
        partner.TransitionStatus(QhinPartnerStatus.Active, DateTime.UtcNow, "dpo");
        partner.Status.ShouldBe(QhinPartnerStatus.Active);
    }

    [Fact]
    public void Duplicate_Anchor_Thumbprint_Is_Rejected()
    {
        var partner = Make_Partner();
        partner.AttachTrustAnchor(Make_Anchor(partner.Id, "AA"));
        Should.Throw<InvalidOperationException>(() =>
            partner.AttachTrustAnchor(Make_Anchor(partner.Id, "AA")));
    }

    [Fact]
    public void Revoke_Anchor_Flips_Status_And_Stamps_Time()
    {
        var partner = Make_Partner();
        var anchor = Make_Anchor(partner.Id, "AA");
        partner.AttachTrustAnchor(anchor);
        var now = new DateTime(2026, 6, 4, 12, 0, 0, DateTimeKind.Utc);
        partner.RevokeTrustAnchor(anchor.Id, now);
        anchor.Status.ShouldBe(TrustAnchorStatus.Revoked);
        anchor.RevokedAtUtc.ShouldBe(now);
    }

    [Fact]
    public void Rotate_Mtls_Updates_Thumbprint_And_Audit()
    {
        var partner = Make_Partner();
        var now = new DateTime(2026, 6, 4, 12, 0, 0, DateTimeKind.Utc);
        partner.RotateMtls("inmem://blobs/v2", "MTLS-NEW", now, "dpo-2");
        partner.MtlsCertStorageRef.ShouldBe("inmem://blobs/v2");
        partner.MtlsCertThumbprint.ShouldBe("MTLS-NEW");
        partner.UpdatedAtUtc.ShouldBe(now);
        partner.UpdatedBy.ShouldBe("dpo-2");
    }

    private static QhinPartner Make_Partner() => new(
        Guid.CreateVersion7(), "Acme QHIN", "https://qhin.example/fhir", "https://qhin.example/ias",
        new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc), "dpo");

    private static QhinTrustAnchor Make_Anchor(Guid partnerId, string thumbprint) => new(
        Guid.CreateVersion7(), partnerId,
        subject: "CN=Acme Root",
        thumbprint: thumbprint,
        certificatePem: "-----BEGIN CERTIFICATE-----stub-----END CERTIFICATE-----",
        notBefore: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        notAfter: new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        attachedAtUtc: DateTime.UtcNow,
        attachedBy: "dpo");
}

using Dialysis.HIE.Documents.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class DocumentReferenceSignatureLtvTests
{
    [Fact]
    public void Default_Construction_Is_Pades_B_Aes()
    {
        var s = Make_Signature();
        s.PadesLevel.ShouldBe(PadesLevel.B);
        s.SignatureFormat.ShouldBe(SignatureFormat.Aes);
        s.TsaUri.ShouldBeNull();
        s.TimestampedAtUtc.ShouldBeNull();
        s.RevocationEvidenceFormat.ShouldBe(RevocationEvidenceFormat.None);
    }

    [Fact]
    public void Pades_T_Requires_Tsa_Uri()
    {
        var ex = Should.Throw<ArgumentException>(() => Make_Signature(level: PadesLevel.T));
        ex.ParamName.ShouldBe("tsaUri");
    }

    [Fact]
    public void Pades_Lt_Requires_Revocation_Evidence()
    {
        var ex = Should.Throw<ArgumentException>(() => Make_Signature(
            level: PadesLevel.Lt,
            tsaUri: "http://timestamp.example/tsa",
            revocationEvidenceFormat: RevocationEvidenceFormat.None));
        ex.ParamName.ShouldBe("revocationEvidenceFormat");
    }

    [Fact]
    public void Qes_Requires_Tsp_Credential_Id()
    {
        var ex = Should.Throw<ArgumentException>(() => Make_Signature(
            signerKind: DocumentSignerKind.RemoteQes,
            tspCredentialId: null));
        ex.ParamName.ShouldBe("tspCredentialId");
    }

    [Fact]
    public void Qes_Format_Requires_Remote_Signer_Kind()
    {
        var ex = Should.Throw<ArgumentException>(() => Make_Signature(
            signatureFormat: SignatureFormat.Qes,
            signerKind: DocumentSignerKind.Platform));
        ex.ParamName.ShouldBe("signerKind");
    }

    [Fact]
    public void Upgrade_Level_Raises_Level_And_Records_Evidence()
    {
        var s = Make_Signature(
            level: PadesLevel.T,
            tsaUri: "http://timestamp.example/tsa",
            timestampedAtUtc: DateTime.UtcNow,
            revocationEvidenceFormat: RevocationEvidenceFormat.Crl,
            revocationEvidenceBlob: [1]);

        s.UpgradeLevel(PadesLevel.Lta, RevocationEvidenceFormat.Both, [2, 3, 4]);

        s.PadesLevel.ShouldBe(PadesLevel.Lta);
        s.RevocationEvidenceFormat.ShouldBe(RevocationEvidenceFormat.Both);
        s.RevocationEvidenceBlob.ShouldBe([2, 3, 4]);
    }

    [Fact]
    public void Upgrade_Level_Below_Current_Throws()
    {
        var s = Make_Signature(
            level: PadesLevel.Lt,
            tsaUri: "http://timestamp.example/tsa",
            timestampedAtUtc: DateTime.UtcNow,
            revocationEvidenceFormat: RevocationEvidenceFormat.Crl,
            revocationEvidenceBlob: [1]);

        Should.Throw<InvalidOperationException>(() =>
            s.UpgradeLevel(PadesLevel.T, RevocationEvidenceFormat.Crl, [1]));
    }

    private static DocumentReferenceSignature Make_Signature(
        DocumentSignerKind signerKind = DocumentSignerKind.Platform,
        PadesLevel level = PadesLevel.B,
        SignatureFormat signatureFormat = SignatureFormat.Aes,
        string? tsaUri = null,
        DateTime? timestampedAtUtc = null,
        RevocationEvidenceFormat revocationEvidenceFormat = RevocationEvidenceFormat.None,
        byte[]? revocationEvidenceBlob = null,
        string? tspCredentialId = "credential-1",
        string? signerUserId = null) =>
        new(
            id: Guid.CreateVersion7(),
            documentReferenceId: Guid.NewGuid(),
            signerKind: signerKind,
            certThumbprint: "AA",
            signedAtUtc: DateTime.UtcNow,
            padesLevel: level,
            signatureFormat: signatureFormat,
            signerUserId: signerUserId,
            tsaUri: tsaUri,
            timestampedAtUtc: timestampedAtUtc,
            revocationEvidenceFormat: revocationEvidenceFormat,
            revocationEvidenceBlob: revocationEvidenceBlob,
            tspId: signerKind == DocumentSignerKind.RemoteQes ? "tsp-x" : null,
            tspCredentialId: signerKind == DocumentSignerKind.RemoteQes ? tspCredentialId : null);
}

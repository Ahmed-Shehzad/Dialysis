using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.HIE.Documents.Features.SignDocument;
using Dialysis.HIE.Documents.Features.UploadDocument;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class DocumentCommandValidatorTests
{
    private readonly SignDocumentCommandValidator _sign = new();
    private readonly UploadDocumentCommandValidator _upload = new();

    [Fact]
    public async Task Sign_Accepts_Platform_Signature_Without_User_Async()
    {
        var cmd = new SignDocumentCommand(
            Guid.NewGuid(), PdfSigningCertificateSource.Platform, UserId: null,
            Reason: "Approval", Location: null, ContactInfo: null);

        (await _sign.ValidateAsync(cmd, CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Sign_Requires_User_Id_For_Per_User_Source_Async()
    {
        var cmd = new SignDocumentCommand(
            Guid.NewGuid(), PdfSigningCertificateSource.User, UserId: null,
            Reason: null, Location: null, ContactInfo: null);

        (await _sign.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Sign_Requires_Tsp_Credential_For_Remote_Qes_Async()
    {
        var cmd = new SignDocumentCommand(
            Guid.NewGuid(), PdfSigningCertificateSource.RemoteQes, UserId: "u1",
            Reason: null, Location: null, ContactInfo: null,
            Level: PadesConformance.B, TspCredentialId: null);

        (await _sign.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Sign_Rejects_Empty_Document_Id_Async()
    {
        var cmd = new SignDocumentCommand(
            Guid.Empty, PdfSigningCertificateSource.Platform, UserId: null,
            Reason: null, Location: null, ContactInfo: null);

        (await _sign.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Upload_Accepts_Valid_Command_Async()
    {
        var content = Convert.ToBase64String("hello"u8.ToArray());
        var cmd = new UploadDocumentCommand(
            Guid.NewGuid(), "DischargeSummary", "Summary", "application/pdf", content,
            LanguageCode: "en", Category: null, CreatedBy: "operator");

        (await _upload.ValidateAsync(cmd, CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not base64 !!!")]
    [InlineData("")]
    public async Task Upload_Rejects_Bad_Base64_Async(string content)
    {
        var cmd = new UploadDocumentCommand(
            Guid.NewGuid(), "DischargeSummary", "Summary", "application/pdf", content,
            LanguageCode: null, Category: null, CreatedBy: null);

        (await _upload.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Upload_Rejects_Empty_Patient_Id_Async()
    {
        var content = Convert.ToBase64String("x"u8.ToArray());
        var cmd = new UploadDocumentCommand(
            Guid.Empty, "Kind", "Title", "text/plain", content,
            LanguageCode: null, Category: null, CreatedBy: null);

        (await _upload.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }
}

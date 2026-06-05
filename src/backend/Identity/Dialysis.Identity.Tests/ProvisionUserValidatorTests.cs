using Dialysis.Identity.Provisioning.Features.ProvisionUser;
using Shouldly;
using Xunit;

namespace Dialysis.Identity.Tests;

public sealed class ProvisionUserValidatorTests
{
    private readonly ProvisionUserCommandValidator _sut = new();

    [Fact]
    public async Task Accepts_Valid_Command_Async()
    {
        var cmd = new ProvisionUserCommand("auth0|abc123", "Dr. Grey", "grey@example.com");

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Accepts_Null_Email_Async()
    {
        var cmd = new ProvisionUserCommand("auth0|abc123", "Dr. Grey", Email: null);

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Dr. Grey", null)]
    [InlineData("auth0|abc", "  ", null)]
    [InlineData("auth0|abc", "Dr. Grey", "not-an-email")]
    [InlineData("auth0|abc", "Dr. Grey", "a@b@c")]
    public async Task Rejects_Invalid_Input_Async(string subject, string displayName, string? email)
    {
        var cmd = new ProvisionUserCommand(subject, displayName, email);

        (await _sut.ValidateAsync(cmd, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }
}

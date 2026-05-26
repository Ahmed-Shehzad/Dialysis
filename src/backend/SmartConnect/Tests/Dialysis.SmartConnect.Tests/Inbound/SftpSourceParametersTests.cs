using Dialysis.SmartConnect.Inbound.Sftp;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Inbound;

/// <summary>
/// Covers parameter parsing + the glob-to-regex helper for <see cref="SftpSourceConnector"/>.
/// Actual SFTP connectivity is covered by an integration test in a follow-up PR (requires the
/// atmoz/sftp Testcontainer).
/// </summary>
public sealed class SftpSourceParametersTests
{
    [Fact]
    public void Parse_Accepts_Password_Auth()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host"] = "sftp.example",
            ["Username"] = "u",
            ["Password"] = "p",
            ["RemoteDirectory"] = "/in",
        };

        var p = SftpSourceParameters.Parse(raw);

        Assert.Equal("sftp.example", p.Host);
        Assert.Equal(22, p.Port);
        Assert.Equal("u", p.Username);
        Assert.Equal("p", p.Password);
        Assert.Null(p.PrivateKeyPath);
        Assert.Equal("/in", p.RemoteDirectory);
        Assert.Equal(SftpAfterReadAction.Delete, p.AfterRead);
    }

    [Fact]
    public void Parse_Accepts_Private_Key_Auth()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host"] = "sftp.example",
            ["Username"] = "u",
            ["PrivateKeyPath"] = "/etc/keys/id_rsa",
            ["RemoteDirectory"] = "/in",
            ["AfterRead"] = "move",
            ["MoveToDirectory"] = "/processed",
        };

        var p = SftpSourceParameters.Parse(raw);

        Assert.Equal("/etc/keys/id_rsa", p.PrivateKeyPath);
        Assert.Null(p.Password);
        Assert.Equal(SftpAfterReadAction.Move, p.AfterRead);
        Assert.Equal("/processed", p.MoveToDirectory);
    }

    [Fact]
    public void Parse_Rejects_When_Both_Password_And_Key()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host"] = "sftp.example",
            ["Username"] = "u",
            ["Password"] = "p",
            ["PrivateKeyPath"] = "/etc/keys/id_rsa",
            ["RemoteDirectory"] = "/in",
        };

        Assert.Throws<ArgumentException>(() => SftpSourceParameters.Parse(raw));
    }

    [Fact]
    public void Parse_Rejects_When_Neither_Password_Nor_Key()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host"] = "sftp.example",
            ["Username"] = "u",
            ["RemoteDirectory"] = "/in",
        };

        Assert.Throws<ArgumentException>(() => SftpSourceParameters.Parse(raw));
    }

    [Fact]
    public void Parse_Rejects_Move_Without_Destination()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host"] = "sftp.example",
            ["Username"] = "u",
            ["Password"] = "p",
            ["RemoteDirectory"] = "/in",
            ["AfterRead"] = "move",
        };

        Assert.Throws<ArgumentException>(() => SftpSourceParameters.Parse(raw));
    }

    [Theory]
    [InlineData("*.hl7", "lab.hl7", true)]
    [InlineData("*.hl7", "lab.xml", false)]
    [InlineData("ORU_*.hl7", "ORU_001.hl7", true)]
    [InlineData("ORU_*.hl7", "ADT_001.hl7", false)]
    [InlineData("ord?.txt", "ord1.txt", true)]
    [InlineData("ord?.txt", "ord12.txt", false)]
    public void Glob_Matches_Expected_File_Names(string pattern, string name, bool expected)
    {
        var rx = SftpSourceConnector.ToRegex(pattern);
        Assert.Equal(expected, rx.IsMatch(name));
    }
}

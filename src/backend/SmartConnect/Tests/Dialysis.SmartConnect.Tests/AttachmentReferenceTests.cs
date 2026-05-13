using Dialysis.SmartConnect.Attachments;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AttachmentReferenceTests
{
    [Fact]
    public void Format_then_parse_roundtrips()
    {
        var id = Guid.Parse("00000000-0000-4000-8000-000000000abc");
        var token = AttachmentReference.Format(id);
        Assert.Equal("${ATTACH:00000000-0000-4000-8000-000000000abc}", token);
        Assert.True(AttachmentReference.TryParseToken(token, out var parsed));
        Assert.Equal(id, parsed);
    }

    [Fact]
    public void TryParse_rejects_malformed()
    {
        Assert.False(AttachmentReference.TryParseToken("${ATTACH:not-a-guid}", out _));
        Assert.False(AttachmentReference.TryParseToken("${ATTACH:}", out _));
        Assert.False(AttachmentReference.TryParseToken("plain text", out _));
    }

    [Fact]
    public void Scan_finds_multiple_tokens_in_order()
    {
        var a = AttachmentReference.Format(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var b = AttachmentReference.Format(Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var text = $"head {a} mid {b} tail";

        var hits = AttachmentReference.Scan(text).ToList();
        Assert.Equal(2, hits.Count);
        Assert.Equal(Guid.Parse("11111111-1111-4111-8111-111111111111"), hits[0].Id);
        Assert.Equal(Guid.Parse("22222222-2222-4222-8222-222222222222"), hits[1].Id);
    }

    [Fact]
    public void Scan_skips_unparseable_tokens()
    {
        var valid = AttachmentReference.Format(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var text = $"${{ATTACH:not-a-guid}} {valid}";
        var hits = AttachmentReference.Scan(text).ToList();
        Assert.Single(hits);
        Assert.Equal(Guid.Parse("11111111-1111-4111-8111-111111111111"), hits[0].Id);
    }
}

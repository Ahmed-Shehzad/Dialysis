using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class OperatorShellSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OperatorShellSmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task IndexHtml_serves_with_expected_nav_anchors()
    {
        using var client = _factory.CreateClient();

        using var res = await client.GetAsync("/smartconnect/index.html");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("text/html", res.Content.Headers.ContentType?.MediaType);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("<title>SmartConnect", body);
        Assert.Contains("href=\"#flows\"", body);
        Assert.Contains("href=\"#alerts\"", body);
        Assert.Contains("href=\"#alert-events\"", body);
        Assert.Contains("href=\"#code-templates\"", body);
        Assert.Contains("href=\"#variable-maps\"", body);
        Assert.Contains("id=\"panel-root\"", body);
        Assert.Contains("id=\"auth-bar-mount\"", body);
        Assert.Contains("/smartconnect/app.js", body);
        Assert.Contains("/smartconnect/app.css", body);
    }

    [Fact]
    public async Task AppJs_bundle_is_served()
    {
        using var client = _factory.CreateClient();

        using var res = await client.GetAsync("/smartconnect/app.js");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var mime = res.Content.Headers.ContentType?.MediaType;
        Assert.True(mime is "text/javascript" or "application/javascript",
            $"unexpected JS content-type '{mime}'");
        var body = await res.Content.ReadAsStringAsync();
        Assert.NotEmpty(body);
    }

    [Fact]
    public async Task AppCss_bundle_is_served()
    {
        using var client = _factory.CreateClient();

        using var res = await client.GetAsync("/smartconnect/app.css");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("text/css", res.Content.Headers.ContentType?.MediaType);
        var body = await res.Content.ReadAsStringAsync();
        Assert.NotEmpty(body);
    }

    [Fact]
    public async Task Root_redirects_to_shell_index()
    {
        var options = new WebApplicationFactoryClientOptions { AllowAutoRedirect = false };
        using var client = _factory.CreateClient(options);

        using var res = await client.GetAsync("/");

        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.MovedPermanently,
            $"expected a redirect, got {(int)res.StatusCode}");
        Assert.Equal("/smartconnect/index.html", res.Headers.Location?.OriginalString);
    }
}

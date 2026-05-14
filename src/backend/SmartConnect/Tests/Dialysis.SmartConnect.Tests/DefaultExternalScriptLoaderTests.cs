using System.Net;
using Dialysis.SmartConnect.Scripts;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class DefaultExternalScriptLoaderTests
{
    [Fact]
    public async Task File_uri_inside_allowed_root_returns_contents()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smartconnect-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var scriptPath = Path.Combine(dir, "f.js");
            await File.WriteAllTextAsync(scriptPath, "var x = 1;");

            var loader = NewLoader(opts => opts.AllowedFileRoots.Add(dir));
            var body = await loader.LoadAsync(new Uri(scriptPath), null, CancellationToken.None);

            Assert.Equal("var x = 1;", body);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task File_uri_outside_allowed_root_throws()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), "smartconnect-ext-" + Guid.NewGuid().ToString("N"));
        var otherDir = Path.Combine(Path.GetTempPath(), "smartconnect-other-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDir);
        Directory.CreateDirectory(otherDir);
        try
        {
            var outside = Path.Combine(otherDir, "evil.js");
            await File.WriteAllTextAsync(outside, "/* */");

            var loader = NewLoader(opts => opts.AllowedFileRoots.Add(rootDir));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                loader.LoadAsync(new Uri(outside), null, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(rootDir, recursive: true);
            Directory.Delete(otherDir, recursive: true);
        }
    }

    [Fact]
    public async Task File_uri_with_no_allowed_roots_throws()
    {
        var loader = NewLoader(_ => { });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(new Uri("file:///tmp/x.js"), null, CancellationToken.None));
    }

    [Fact]
    public async Task Unknown_scheme_throws()
    {
        var loader = NewLoader(_ => { });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(new Uri("ftp://example/x.js"), null, CancellationToken.None));
    }

    [Fact]
    public async Task Cache_returns_first_body_within_ttl()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smartconnect-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "f.js");
            await File.WriteAllTextAsync(path, "first");

            var time = new FixedTimeProvider(new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero));
            var loader = NewLoader(opts => opts.AllowedFileRoots.Add(dir), time);

            var first = await loader.LoadAsync(new Uri(path), TimeSpan.FromMinutes(5), CancellationToken.None);
            await File.WriteAllTextAsync(path, "second");
            var second = await loader.LoadAsync(new Uri(path), TimeSpan.FromMinutes(5), CancellationToken.None);

            Assert.Equal("first", first);
            Assert.Equal("first", second);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Cache_refreshes_after_ttl_elapses()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smartconnect-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "f.js");
            await File.WriteAllTextAsync(path, "first");

            var time = new FixedTimeProvider(new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero));
            var loader = NewLoader(opts => opts.AllowedFileRoots.Add(dir), time);

            var first = await loader.LoadAsync(new Uri(path), TimeSpan.FromSeconds(10), CancellationToken.None);
            await File.WriteAllTextAsync(path, "second");
            time.Advance(TimeSpan.FromSeconds(30));
            var second = await loader.LoadAsync(new Uri(path), TimeSpan.FromSeconds(10), CancellationToken.None);

            Assert.Equal("first", first);
            Assert.Equal("second", second);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Http_uri_to_disallowed_host_throws()
    {
        var loader = NewLoader(opts => opts.AllowedHttpHosts.Add("good.example"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            loader.LoadAsync(new Uri("https://evil.example/x.js"), null, CancellationToken.None));
    }

    [Fact]
    public async Task File_uri_exceeding_max_bytes_throws()
    {
        var dir = Path.Combine(Path.GetTempPath(), "smartconnect-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "f.js");
            await File.WriteAllTextAsync(path, new string('x', 1024));
            var loader = NewLoader(opts =>
            {
                opts.AllowedFileRoots.Add(dir);
                opts.MaxScriptBytes = 128;
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                loader.LoadAsync(new Uri(path), null, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static DefaultExternalScriptLoader NewLoader(Action<ExternalScriptOptions> configure, TimeProvider? time = null)
    {
        var opts = new ExternalScriptOptions();
        configure(opts);
        var monitor = new StaticOptionsMonitor<ExternalScriptOptions>(opts);
        return new DefaultExternalScriptLoader(monitor, new StubHttpClientFactory(), time ?? TimeProvider.System);
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new ConstantHandler());
    }

    private sealed class ConstantHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private sealed class FixedTimeProvider(DateTimeOffset startUtc) : TimeProvider
    {
        private DateTimeOffset _now = startUtc;
        public void Advance(TimeSpan delta) => _now += delta;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}

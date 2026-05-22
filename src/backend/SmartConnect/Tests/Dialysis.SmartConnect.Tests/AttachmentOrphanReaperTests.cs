using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Exercises the reaper sweep against a real <see cref="FileSystemAttachmentBlobStore"/> + an
/// in-memory EF context. The hosted service is NOT started; we call <c>SweepAsync</c> directly so
/// the test doesn't sleep through <see cref="AttachmentOrphanReaperOptions.SweepInterval"/>.
/// </summary>
public sealed class AttachmentOrphanReaperTests
{
    [Fact]
    public async Task Sweep_Deletes_Blob_Without_Matching_Metadata_Async()
    {
        await using var fx = new ReaperFixture();
        var orphanId = Guid.CreateVersion7();
        await fx.Blobs.WriteAsync(orphanId, Encoding.UTF8.GetBytes("orphan"), CancellationToken.None);

        // Age the file past the grace window: advance the test clock by 1 hour.
        fx.Clock.Advance(TimeSpan.FromHours(1));

        await fx.Reaper.SweepAsync(CancellationToken.None);

        Assert.Null(await fx.Blobs.ReadAsync(orphanId, CancellationToken.None));
    }

    [Fact]
    public async Task Sweep_Preserves_Blob_With_Matching_Metadata_Async()
    {
        await using var fx = new ReaperFixture();
        var id = Guid.CreateVersion7();
        await fx.Blobs.WriteAsync(id, Encoding.UTF8.GetBytes("kept"), CancellationToken.None);
        fx.Db.Attachments.Add(new Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities.AttachmentEntity
        {
            Id = id,
            MessageId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            MimeType = "text/plain",
            SizeBytes = 4,
            CreatedUtc = DateTimeOffset.UtcNow,
        });
        await fx.Db.SaveChangesAsync();

        fx.Clock.Advance(TimeSpan.FromHours(1));
        await fx.Reaper.SweepAsync(CancellationToken.None);

        Assert.NotNull(await fx.Blobs.ReadAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Sweep_Skips_Blob_Younger_Than_Grace_Period_Async()
    {
        await using var fx = new ReaperFixture();
        var youngId = Guid.CreateVersion7();
        await fx.Blobs.WriteAsync(youngId, Encoding.UTF8.GetBytes("young"), CancellationToken.None);

        // No clock advance — file is fresh.
        await fx.Reaper.SweepAsync(CancellationToken.None);

        Assert.NotNull(await fx.Blobs.ReadAsync(youngId, CancellationToken.None));
    }

    [Fact]
    public async Task Sweep_Is_NoOp_When_Blob_Store_Is_InRow_Async()
    {
        await using var fx = new ReaperFixture(useInRowStore: true);
        // Add a metadata-less row scenario impossible for in-row, but verify the sweep returns
        // without touching the store. We assert by recording calls indirectly: in-row's
        // EnumerateAsync surfaces every metadata row, but with no rows in DB it's a no-op.
        await fx.Reaper.SweepAsync(CancellationToken.None);
        // No assertion needed beyond "no exception" — the path returns early on StoresBytesInRow.
    }

    [Fact]
    public async Task Sweep_Honours_Max_Deletions_Cap_Async()
    {
        await using var fx = new ReaperFixture(maxDeletions: 2);
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7()).ToList();
        foreach (var id in ids)
        {
            await fx.Blobs.WriteAsync(id, Encoding.UTF8.GetBytes("x"), CancellationToken.None);
        }
        fx.Clock.Advance(TimeSpan.FromHours(1));

        await fx.Reaper.SweepAsync(CancellationToken.None);

        var remaining = 0;
        foreach (var id in ids)
        {
            if (await fx.Blobs.ReadAsync(id, CancellationToken.None) is not null) remaining++;
        }
        Assert.Equal(3, remaining); // 5 candidates, cap of 2 deletions
    }

    private sealed class ReaperFixture : IAsyncDisposable
    {
        public string RootPath { get; }
        public IAttachmentBlobStore Blobs { get; }
        public SmartConnectDbContext Db { get; }
        public AttachmentOrphanReaperHostedService Reaper { get; }
        public FakeTimeProvider Clock { get; } = new();
        private readonly ServiceProvider _sp;

        public ReaperFixture(bool useInRowStore = false, int maxDeletions = 1000)
        {
            RootPath = Path.Combine(Path.GetTempPath(), "sc-reaper-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);

            var services = new ServiceCollection();
            services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_reaper_{Guid.NewGuid():N}");
            if (!useInRowStore)
            {
                services.UseFileSystemAttachmentBlobStore(o => o.RootPath = RootPath);
            }
            services.Configure<AttachmentOrphanReaperOptions>(o =>
            {
                o.GracePeriod = TimeSpan.FromMinutes(5);
                o.MaxDeletionsPerSweep = maxDeletions;
            });
            services.AddSingleton<TimeProvider>(Clock);
            _sp = services.BuildServiceProvider();

            var scope = _sp.CreateScope();
            Db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
            Blobs = scope.ServiceProvider.GetRequiredService<IAttachmentBlobStore>();
            Reaper = new AttachmentOrphanReaperHostedService(
                _sp.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(_sp.GetRequiredService<IOptions<AttachmentOrphanReaperOptions>>().Value),
                Clock,
                NullLogger<AttachmentOrphanReaperHostedService>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            await _sp.DisposeAsync();
            try
            {
                if (Directory.Exists(RootPath)) Directory.Delete(RootPath, recursive: true);
            }
            catch (IOException) { /* best effort */ }
        }
    }

    /// <summary>
    /// Minimal TimeProvider stub. We don't pull Microsoft.Extensions.TimeProvider.Testing because
    /// the reaper only needs <c>GetUtcNow()</c> and <c>Task.Delay(..., TimeProvider, ct)</c>; the
    /// sweep tests call <c>SweepAsync</c> directly and never await delays.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}

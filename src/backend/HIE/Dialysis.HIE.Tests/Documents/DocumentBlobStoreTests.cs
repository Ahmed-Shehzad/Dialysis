using Dialysis.BuildingBlocks.Documents.Storage;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents;

public sealed class DocumentBlobStoreTests
{
    [Fact]
    public async Task In_Memory_Round_Trips_Bytes_Async()
    {
        var sut = new InMemoryDocumentBlobStore();
        var id = Guid.CreateVersion7();
        var bytes = new byte[] { 1, 2, 3, 4 };

        var storageRef = await sut.SaveAsync(id, "application/pdf", bytes, CancellationToken.None);
        var roundTripped = await sut.ReadAsync(storageRef, CancellationToken.None);

        storageRef.ShouldStartWith("inmem://documents/");
        roundTripped.ShouldBe(bytes);
    }

    [Fact]
    public async Task In_Memory_Delete_Removes_Bytes_Async()
    {
        var sut = new InMemoryDocumentBlobStore();
        var id = Guid.CreateVersion7();
        var storageRef = await sut.SaveAsync(id, "application/pdf", new byte[] { 0 }, CancellationToken.None);

        var removed = await sut.DeleteAsync(storageRef, CancellationToken.None);
        var afterDelete = await sut.ReadAsync(storageRef, CancellationToken.None);

        removed.ShouldBeTrue();
        afterDelete.ShouldBeNull();
    }

    [Fact]
    public async Task File_System_Rejects_Traversal_Refs_Async()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"docstore-{Guid.NewGuid():N}");
        try
        {
            var options = Options.Create(new FileSystemDocumentBlobStoreOptions { RootPath = dir });
            var sut = new FileSystemDocumentBlobStore(options);

            var legit = await sut.SaveAsync(Guid.CreateVersion7(), "application/pdf", new byte[] { 9 }, CancellationToken.None);
            var legitRead = await sut.ReadAsync(legit, CancellationToken.None);
            legitRead.ShouldBe(new byte[] { 9 });

            var traversalAttempt = await sut.ReadAsync("file://../../../etc/passwd", CancellationToken.None);
            traversalAttempt.ShouldBeNull();
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}

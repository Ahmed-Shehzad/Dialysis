using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Fhir.BulkData.ObjectStorage;

/// <summary>
/// <see cref="IBulkDataStorage"/> over S3 / MinIO. Writes buffer to a temp file and upload on close
/// (the S3 SDK has no native writable stream); reads stream the object body. Object key layout is
/// <c>{prefix}{jobId}/{resourceType}.ndjson</c>, mirroring the local-file sink's directory layout.
/// </summary>
public sealed class S3BulkDataStorage : IBulkDataStorage, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _prefix;

    /// <summary>Builds the client from options (ambient credential chain unless keys are supplied).</summary>
    public S3BulkDataStorage(IOptions<S3BulkDataStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var o = options.Value;
        _bucket = o.BucketName;
        _prefix = o.KeyPrefix ?? string.Empty;

        var config = new AmazonS3Config { ForcePathStyle = o.ForcePathStyle };
        if (!string.IsNullOrWhiteSpace(o.ServiceUrl))
            config.ServiceURL = o.ServiceUrl;
        else
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(o.Region);

        _client = string.IsNullOrWhiteSpace(o.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(o.AccessKey, o.SecretKey, config);
    }

    /// <inheritdoc />
    public ValueTask<Stream> OpenWriteAsync(string jobId, string resourceType, CancellationToken cancellationToken)
    {
        var key = KeyOf(jobId, resourceType + ".ndjson");
        var tempPath = Path.Combine(Path.GetTempPath(), "fhir-bulk-" + Guid.NewGuid().ToString("N") + ".ndjson");
        var file = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, useAsync: true);
        return ValueTask.FromResult<Stream>(new UploadOnCloseStream(file, tempPath, _client, _bucket, key));
    }

    /// <inheritdoc />
    public async ValueTask<Stream> OpenReadAsync(string jobId, string fileName, CancellationToken cancellationToken)
    {
        var response = await _client.GetObjectAsync(_bucket, KeyOf(jobId, fileName), cancellationToken).ConfigureAwait(false);
        return response.ResponseStream;
    }

    /// <inheritdoc />
    public string BuildOutputUrl(string jobId, string resourceType) =>
        $"s3://{_bucket}/{KeyOf(jobId, resourceType + ".ndjson")}";

    private string KeyOf(string jobId, string fileName) => $"{_prefix}{Sanitize(jobId)}/{Sanitize(fileName)}";

    private static string Sanitize(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment)
            || segment != Path.GetFileName(segment)
            || segment.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(segment))
        {
            throw new ArgumentException($"Invalid bulk-data path segment: '{segment}'.", nameof(segment));
        }
        return segment;
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    /// <summary>A writable temp-file stream that PUTs its contents to S3 on async close.</summary>
    private sealed class UploadOnCloseStream : Stream
    {
        private readonly FileStream _file;
        private readonly string _tempPath;
        private readonly AmazonS3Client _client;
        private readonly string _bucket;
        private readonly string _key;
        private bool _uploaded;

        public UploadOnCloseStream(FileStream file, string tempPath, AmazonS3Client client, string bucket, string key)
        {
            _file = file;
            _tempPath = tempPath;
            _client = client;
            _bucket = bucket;
            _key = key;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _file.Length;
        public override long Position { get => _file.Position; set => throw new NotSupportedException(); }
        public override void Flush() => _file.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _file.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => _file.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _file.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            _file.WriteAsync(buffer, cancellationToken);

        public override async ValueTask DisposeAsync()
        {
            await UploadAsync().ConfigureAwait(false);
            await _file.DisposeAsync().ConfigureAwait(false);
            TryDeleteTemp();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // The writer (ExportJobRunner) disposes via `await using`, so DisposeAsync is the real
                // path. This synchronous fallback only runs for a plain `using`; the S3 SDK is
                // async-only so blocking here is unavoidable (and deadlock-safe — no captured context).
#pragma warning disable VSTHRD002 // Synchronously waiting on tasks — see comment above.
                UploadAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                _file.Dispose();
                TryDeleteTemp();
            }
            base.Dispose(disposing);
        }

        private async Task UploadAsync()
        {
            if (_uploaded) return;
            _uploaded = true;
            await _file.FlushAsync().ConfigureAwait(false);
            _file.Position = 0;
            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = _key,
                InputStream = _file,
                ContentType = "application/fhir+ndjson",
                AutoCloseStream = false,
            };
            await _client.PutObjectAsync(request).ConfigureAwait(false);
        }

        private void TryDeleteTemp()
        {
            try { File.Delete(_tempPath); }
            catch { /* best-effort temp cleanup */ }
        }
    }
}

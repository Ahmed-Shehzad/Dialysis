using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.AzureBlob;

/// <summary>
/// Mints Azure Storage SAS (Shared Access Signature) URLs for attachments stored via
/// <see cref="AzureBlobAttachmentBlobStore"/>. Works against both Azurite (connection-string mode)
/// and real Azure Storage (managed-identity user-delegation SAS).
/// </summary>
public sealed class AzureBlobSasAttachmentDownloadUrlFactory : IAttachmentDownloadUrlFactory
{
    private readonly BlobContainerClient _container;
    private readonly BlobServiceClient? _serviceClient;
    private readonly string _prefix;

    public AzureBlobSasAttachmentDownloadUrlFactory(IOptions<AzureBlobAttachmentBlobOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ContainerName))
        {
            throw new ArgumentException("ContainerName must be configured.", nameof(options));
        }
        _prefix = string.IsNullOrWhiteSpace(opts.KeyPrefix)
            ? string.Empty
            : (opts.KeyPrefix.EndsWith('/') ? opts.KeyPrefix : opts.KeyPrefix + "/");

        if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
        {
            _container = new BlobContainerClient(opts.ConnectionString, opts.ContainerName);
            _serviceClient = null;
        }
        else if (opts.ServiceUri is not null)
        {
            _serviceClient = new BlobServiceClient(opts.ServiceUri, new DefaultAzureCredential());
            _container = _serviceClient.GetBlobContainerClient(opts.ContainerName);
        }
        else
        {
            throw new ArgumentException(
                "Either ConnectionString or ServiceUri must be set.", nameof(options));
        }
    }

    public bool SupportsSignedUrls => true;

    public async Task<Uri?> CreateAsync(Guid attachmentId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var key = _prefix + attachmentId.ToString("D");
        var blob = _container.GetBlobClient(key);
        var expiresOn = DateTimeOffset.UtcNow.Add(ttl);
        var permissions = BlobSasPermissions.Read;

        if (_serviceClient is not null)
        {
            // Managed-identity flow — request a user-delegation key from the Storage account, then
            // sign locally with it. Avoids needing the account key in process.
            var userDelegationKey = await _serviceClient.GetUserDelegationKeyAsync(
                startsOn: DateTimeOffset.UtcNow.AddMinutes(-5), expiresOn, cancellationToken).ConfigureAwait(false);
            var sasBuilder = new BlobSasBuilder(permissions, expiresOn)
            {
                BlobContainerName = _container.Name,
                BlobName = key,
                Protocol = SasProtocol.Https,
            };
            var sas = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _container.AccountName);
            var uri = new BlobUriBuilder(blob.Uri) { Sas = sas };
            return uri.ToUri();
        }

        return blob.CanGenerateSasUri
            ? blob.GenerateSasUri(permissions, expiresOn)
            : null;
    }
}

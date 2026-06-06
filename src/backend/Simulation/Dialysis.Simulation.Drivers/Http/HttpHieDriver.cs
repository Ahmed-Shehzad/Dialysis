namespace Dialysis.Simulation.Drivers.Http;

/// <summary>Drives the real HIE document store.</summary>
public sealed class HttpHieDriver : IHieDriver
{
    private readonly HttpClient _client;

    /// <summary>Creates the driver.</summary>
    public HttpHieDriver(HttpClient client) => _client = client;

    /// <inheritdoc />
    public async Task<UploadedDocument> UploadDocumentAsync(UploadDocumentCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = await HttpDriverJson.PostReadIdAsync(_client, "api/v1.0/documents",
            new
            {
                command.PatientId,
                command.Kind,
                command.Title,
                command.MimeType,
                Base64Content = Convert.ToBase64String(command.Content),
            },
            context, cancellationToken).ConfigureAwait(false);
        return new UploadedDocument(id);
    }
}

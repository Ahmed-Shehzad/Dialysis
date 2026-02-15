using System.Net.Http;
using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for Documents service – fetch PDF content by DocumentReference ID.</summary>
public interface IDocumentsApi
{
    [Get("api/v1/documents/{id}/content")]
    Task<HttpResponseMessage> GetContentAsync(string id, CancellationToken cancellationToken = default);
}

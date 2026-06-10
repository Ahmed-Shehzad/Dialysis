using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Documents.Signing.Csc;

/// <summary>
/// Minimal Cloud Signature Consortium v2 client. Talks to a remote TSP holding a qualified
/// signing credential, fetches the public cert for a credentialId, exchanges client
/// credentials for an SAD (Signature Activation Data) token, and submits document hashes
/// to <c>/signatures/signHash</c> for remote PKCS#7 signing. No third-party CSC library
/// for .NET exists, so this is hand-rolled — kept small and focused on the v2 endpoints
/// we actually call.
///
/// Network resilience and access-token caching are provided by the consumer-supplied
/// <see cref="HttpClient"/> (registered with Polly via <c>AddResilientModuleHttpClient</c>)
/// and <see cref="IDistributedCache"/> — mirrors the OAuth2 token-acquirer pattern under
/// <c>SmartConnect/Adapters/Dialysis.SmartConnect.Adapters.Common/OAuth2TokenAcquirer.cs</c>.
/// </summary>
public sealed class CscV2Client
{
    public const string HttpClientName = "documents-signing-csc";

    private readonly HttpClient _httpClient;
    private readonly CscV2Options _options;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CscV2Client> _logger;

    public CscV2Client(
        HttpClient httpClient,
        IOptions<CscV2Options> options,
        IDistributedCache cache,
        ILogger<CscV2Client> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public string TspId => _options.TspId;

    /// <summary>
    /// Returns the credential record (cert chain + worst-case signature size) for
    /// <paramref name="credentialId"/> via CSC <c>/credentials/info</c>.
    /// </summary>
    public async Task<CredentialInfoResponse> GetCredentialInfoAsync(string credentialId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
        var access = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        using var message = new HttpRequestMessage(HttpMethod.Post, ResolveUri("credentials/info"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        message.Content = JsonContent.Create(new CredentialInfoRequest(credentialId, Certificates: "chain", CertInfo: true, AuthInfo: true));
        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CredentialInfoResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("CSC /credentials/info returned an empty body.");
    }

    /// <summary>
    /// Authorises the credential (returns SAD) via CSC <c>/credentials/authorize</c>, then
    /// posts the document hash to <c>/signatures/signHash</c> and returns the PKCS#7 bytes.
    /// </summary>
    public async Task<byte[]> SignHashAsync(
        string credentialId,
        ReadOnlyMemory<byte> hash,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
        var access = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var sad = await AuthorizeCredentialAsync(credentialId, access, cancellationToken).ConfigureAwait(false);

        using var signMessage = new HttpRequestMessage(HttpMethod.Post, ResolveUri("signatures/signHash"));
        signMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        signMessage.Content = JsonContent.Create(new SignHashRequest(
            CredentialId: credentialId,
            Sad: sad,
            Hash: [Convert.ToBase64String(hash.Span)],
            HashAlgo: _options.HashAlgorithmOid,
            SignAlgo: _options.SignAlgorithmOid));
        using var signResponse = await _httpClient.SendAsync(signMessage, cancellationToken).ConfigureAwait(false);
        signResponse.EnsureSuccessStatusCode();
        var signResult = await signResponse.Content.ReadFromJsonAsync<SignHashResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("CSC /signatures/signHash returned an empty body.");
        if (signResult.Signatures.Count == 0)
            throw new InvalidOperationException("CSC /signatures/signHash returned no signatures.");
        return Convert.FromBase64String(signResult.Signatures[0]);
    }

    private async Task<string> AuthorizeCredentialAsync(string credentialId, string accessToken, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ResolveUri("credentials/authorize"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Content = JsonContent.Create(new AuthorizeRequest(credentialId, NumSignatures: 1));
        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthorizeResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("CSC /credentials/authorize returned an empty body.");
        if (string.IsNullOrWhiteSpace(body.Sad))
            throw new InvalidOperationException("CSC /credentials/authorize did not return an SAD.");
        return body.Sad;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "documents-signing:csc:access-token";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(cached)) return cached;

        if (string.IsNullOrWhiteSpace(_options.ClientCredentialsTokenUri) ||
            string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                "Documents:Signing:Tsp:{ClientCredentialsTokenUri,ClientId,ClientSecret} are required for the QES path.");
        }

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _options.ClientCredentialsTokenUri);
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId!,
            ["client_secret"] = _options.ClientSecret!,
            ["scope"] = _options.Scope,
        });
        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("OAuth2 token endpoint returned an empty body.");
        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("OAuth2 token endpoint did not return an access_token.");

        var ttl = TimeSpan.FromSeconds(Math.Max(60, token.ExpiresIn - 30));
        await _cache.SetStringAsync(cacheKey, token.AccessToken!,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogDebug("CSC access token cached for {Ttl} seconds.", ttl.TotalSeconds);
        return token.AccessToken!;
    }

    private Uri ResolveUri(string suffix)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUri))
            throw new InvalidOperationException("Documents:Signing:Tsp:BaseUri is not configured.");
        var baseUri = _options.BaseUri!.TrimEnd('/');
        return new Uri($"{baseUri}/{suffix}");
    }

    // -------- Wire shapes --------

    public sealed record CredentialInfoRequest
    {
        public CredentialInfoRequest(string CredentialId,
            string Certificates,
            bool CertInfo,
            bool AuthInfo)
        {
            this.CredentialId = CredentialId;
            this.Certificates = Certificates;
            this.CertInfo = CertInfo;
            this.AuthInfo = AuthInfo;
        }

        [JsonPropertyName("credentialID")]
        public string CredentialId { get; init; }

        [JsonPropertyName("certificates")]
        public string Certificates { get; init; }

        [JsonPropertyName("certInfo")]
        public bool CertInfo { get; init; }

        [JsonPropertyName("authInfo")]
        public bool AuthInfo { get; init; }

        public void Deconstruct(out string CredentialId, out string Certificates, out bool CertInfo, out bool AuthInfo)
        {
            CredentialId = this.CredentialId;
            Certificates = this.Certificates;
            CertInfo = this.CertInfo;
            AuthInfo = this.AuthInfo;
        }
    }

    public sealed record CredentialInfoResponse
    {
        public CredentialInfoResponse(CredentialCertInfo? Cert,
            CredentialKeyInfo? Key)
        {
            this.Cert = Cert;
            this.Key = Key;
        }
        [JsonPropertyName("cert")] public CredentialCertInfo? Cert { get; init; }
        [JsonPropertyName("key")] public CredentialKeyInfo? Key { get; init; }
        public void Deconstruct(out CredentialCertInfo? Cert, out CredentialKeyInfo? Key)
        {
            Cert = this.Cert;
            Key = this.Key;
        }
    }

    public sealed record CredentialCertInfo
    {
        public CredentialCertInfo(IReadOnlyList<string> Certificates,
            string? SubjectDistinguishedName,
            string? Status)
        {
            this.Certificates = Certificates;
            this.SubjectDistinguishedName = SubjectDistinguishedName;
            this.Status = Status;
        }

        [JsonPropertyName("certificates")]
        public IReadOnlyList<string> Certificates { get; init; }

        [JsonPropertyName("subjectDN")]
        public string? SubjectDistinguishedName { get; init; }

        [JsonPropertyName("status")] public string? Status { get; init; }
        public void Deconstruct(out IReadOnlyList<string> Certificates, out string? SubjectDistinguishedName, out string? Status)
        {
            Certificates = this.Certificates;
            SubjectDistinguishedName = this.SubjectDistinguishedName;
            Status = this.Status;
        }
    }

    public sealed record CredentialKeyInfo
    {
        public CredentialKeyInfo(IReadOnlyList<string>? Algo,
            int? Len)
        {
            this.Algo = Algo;
            this.Len = Len;
        }
        [JsonPropertyName("algo")] public IReadOnlyList<string>? Algo { get; init; }
        [JsonPropertyName("len")] public int? Len { get; init; }
        public void Deconstruct(out IReadOnlyList<string>? Algo, out int? Len)
        {
            Algo = this.Algo;
            Len = this.Len;
        }
    }

    private sealed record AuthorizeRequest
    {
        public AuthorizeRequest(string CredentialId,
            int NumSignatures)
        {
            this.CredentialId = CredentialId;
            this.NumSignatures = NumSignatures;
        }

        [JsonPropertyName("credentialID")]
        public string CredentialId { get; init; }

        [JsonPropertyName("numSignatures")]
        public int NumSignatures { get; init; }

        public void Deconstruct(out string CredentialId, out int NumSignatures)
        {
            CredentialId = this.CredentialId;
            NumSignatures = this.NumSignatures;
        }
    }

    private sealed record AuthorizeResponse
    {
        public AuthorizeResponse(string? Sad) => this.Sad = Sad;
        [JsonPropertyName("SAD")] public string? Sad { get; init; }
        public void Deconstruct(out string? Sad) => Sad = this.Sad;
    }

    private sealed record SignHashRequest
    {
        public SignHashRequest(string CredentialId,
            string Sad,
            IReadOnlyList<string> Hash,
            string HashAlgo,
            string SignAlgo)
        {
            this.CredentialId = CredentialId;
            this.Sad = Sad;
            this.Hash = Hash;
            this.HashAlgo = HashAlgo;
            this.SignAlgo = SignAlgo;
        }

        [JsonPropertyName("credentialID")]
        public string CredentialId { get; init; }

        [JsonPropertyName("SAD")] public string Sad { get; init; }
        [JsonPropertyName("hash")] public IReadOnlyList<string> Hash { get; init; }

        [JsonPropertyName("hashAlgo")]
        public string HashAlgo { get; init; }

        [JsonPropertyName("signAlgo")]
        public string SignAlgo { get; init; }

        public void Deconstruct(out string CredentialId, out string Sad, out IReadOnlyList<string> Hash, out string HashAlgo, out string SignAlgo)
        {
            CredentialId = this.CredentialId;
            Sad = this.Sad;
            Hash = this.Hash;
            HashAlgo = this.HashAlgo;
            SignAlgo = this.SignAlgo;
        }
    }

    private sealed record SignHashResponse
    {
        public SignHashResponse(IReadOnlyList<string> Signatures) => this.Signatures = Signatures;

        [JsonPropertyName("signatures")]
        public IReadOnlyList<string> Signatures { get; init; }

        public void Deconstruct(out IReadOnlyList<string> Signatures) => Signatures = this.Signatures;
    }

    private sealed record TokenResponse
    {
        public TokenResponse(string? AccessToken,
            string? TokenType,
            int ExpiresIn)
        {
            this.AccessToken = AccessToken;
            this.TokenType = TokenType;
            this.ExpiresIn = ExpiresIn;
        }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        public void Deconstruct(out string? AccessToken, out string? TokenType, out int ExpiresIn)
        {
            AccessToken = this.AccessToken;
            TokenType = this.TokenType;
            ExpiresIn = this.ExpiresIn;
        }
    }
}

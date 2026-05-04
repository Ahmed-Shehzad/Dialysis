namespace Dialysis.HIS.Integration.External;

/// <summary>ACL-shaped HTTP client for a vendor pharmacy system or mock harness.</summary>
public sealed class HttpPharmacyGateway(HttpClient http) : IPharmacyGateway
{
    public async Task<string> SubmitOrderStubAsync(string medicationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http
                .GetAsync($"stub/pharmacy-orders?code={Uri.EscapeDataString(medicationCode)}", cancellationToken)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(text))
                    return text.Trim();
            }
        }
        catch (HttpRequestException)
        {
            // fall through
        }

        return $"RX_HTTP:{medicationCode}";
    }
}

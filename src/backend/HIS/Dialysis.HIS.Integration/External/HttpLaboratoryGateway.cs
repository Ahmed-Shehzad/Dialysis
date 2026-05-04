using System.Text.Json;

namespace Dialysis.HIS.Integration.External;

/// <summary>ACL-shaped HTTP client for a vendor LIS or internal lab harness (paths are best-effort; 404 falls back to deterministic text).</summary>
public sealed class HttpLaboratoryGateway(HttpClient http) : ILaboratoryGateway
{
    public async Task<string> RequestResultStubAsync(string labOrderId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http
                .GetAsync($"stub/lab-results?orderId={Uri.EscapeDataString(labOrderId)}", cancellationToken)
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

        return $"LAB_HTTP:{labOrderId}";
    }

    public async Task NotifyReferralCreatedStubAsync(Guid referralId, string referralTypeCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { referralId, referralTypeCode });
            using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            _ = await http.PostAsync("stub/lab-referrals", content, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // best-effort notification
        }
    }
}

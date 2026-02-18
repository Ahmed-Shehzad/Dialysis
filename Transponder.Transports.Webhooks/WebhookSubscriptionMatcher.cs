namespace Transponder.Transports.Webhooks;

internal static class WebhookSubscriptionMatcher
{
    public static bool Matches(WebhookSubscription subscription, string messageType)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(messageType);

        IReadOnlyList<string> eventTypes = subscription.EventTypes;
        if (eventTypes.Count == 0) return true;

        foreach (string pattern in eventTypes)
        {
            if (string.IsNullOrWhiteSpace(pattern)) continue;

            string trimmed = pattern.Trim();
            if (trimmed == "*") return true;

            if (trimmed.EndsWith('*'))
            {
                string prefix = trimmed[..^1];
                if (messageType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (string.Equals(messageType, trimmed, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}

namespace Transponder.Transports.Webhooks;

internal static class WebhookSubscriptionMatcher
{
    public static bool Matches(WebhookSubscription subscription, string messageType)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(messageType);

        var eventTypes = subscription.EventTypes;
        if (eventTypes.Count == 0) return true;

        foreach (var pattern in eventTypes)
        {
            if (string.IsNullOrWhiteSpace(pattern)) continue;

            var trimmed = pattern.Trim();
            if (trimmed == "*") return true;

            if (trimmed.EndsWith('*'))
            {
                var prefix = trimmed[..^1];
                if (messageType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (string.Equals(messageType, trimmed, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}

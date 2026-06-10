using System.Text.RegularExpressions;

namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Mirth-flavored alert variable substitution (UG p319). Substitutes <c>${name}</c> tokens with
/// values derived from the firing rule + event. Unknown tokens stay literal.
/// </summary>
public static class AlertVariables
{
    private static readonly Regex _tokenRegex = new(@"\$\{(?<name>[A-Za-z][A-Za-z0-9_]*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Render(string? template, AlertEvent evt, AlertRule rule)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;
        return _tokenRegex.Replace(template, m => Lookup(m.Groups["name"].Value, evt, rule) ?? m.Value);
    }

    private static string? Lookup(string name, AlertEvent evt, AlertRule rule) => name switch
    {
        "alertId" => evt.Id.ToString(),
        "ruleId" => rule.Id.ToString(),
        "ruleName" => rule.Name ?? string.Empty,
        "flowId" => evt.FlowId?.ToString() ?? string.Empty,
        "messageId" => evt.MessageId?.ToString() ?? string.Empty,
        "correlationId" => evt.CorrelationId ?? string.Empty,
        "errorType" => evt.ErrorType.ToString(),
        "errorDetail" => evt.ErrorDetail ?? string.Empty,
        "timestamp" => evt.OccurredAtUtc.ToString("O"),
        _ => null,
    };
}

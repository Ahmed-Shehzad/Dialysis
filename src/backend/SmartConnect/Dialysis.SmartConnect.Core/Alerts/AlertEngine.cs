using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Default <see cref="IAlertSink"/>. On publish, loads enabled rules, filters by flow scope and
/// error type/regex, applies an in-memory throttle window per (rule, flow, errorType) tuple, and
/// runs each matching rule's actions sequentially via <see cref="IFlowPluginRegistry"/>. The single
/// <see cref="AlertEvent"/> recording the outcome is appended to <see cref="IAlertEventStore"/>.
/// </summary>
public sealed class AlertEngine : IAlertSink
{
    private static readonly TimeSpan _defaultThrottleWindow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(3);

    private readonly ConcurrentDictionary<(Guid RuleId, Guid FlowId, AlertErrorType ErrorType), DateTimeOffset> _lastFiredUtc = new();
    private readonly IAlertRuleRepository _rules;
    private readonly IAlertEventStore _events;
    private readonly IFlowPluginRegistry _plugins;
    private readonly TimeProvider _time;
    private readonly ILogger<AlertEngine>? _logger;
    /// <summary>
    /// Default <see cref="IAlertSink"/>. On publish, loads enabled rules, filters by flow scope and
    /// error type/regex, applies an in-memory throttle window per (rule, flow, errorType) tuple, and
    /// runs each matching rule's actions sequentially via <see cref="IFlowPluginRegistry"/>. The single
    /// <see cref="AlertEvent"/> recording the outcome is appended to <see cref="IAlertEventStore"/>.
    /// </summary>
    public AlertEngine(IAlertRuleRepository rules,
        IAlertEventStore events,
        IFlowPluginRegistry plugins,
        TimeProvider time,
        ILogger<AlertEngine>? logger = null)
    {
        _rules = rules;
        _events = events;
        _plugins = plugins;
        _time = time;
        _logger = logger;
    }

    public async Task PublishAsync(AlertTrigger trigger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trigger);

        IReadOnlyList<AlertRule> enabled;
        try
        {
            enabled = await _rules.GetEnabledAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Alert engine failed to load enabled rules.");
            return;
        }

        if (enabled.Count == 0)
            return;

        var nowUtc = _time.GetUtcNow();
        foreach (var rule in enabled)
        {
            if (!Scopes(rule, trigger))
                continue;
            if (!MatchesAnyPattern(rule, trigger))
                continue;
            if (IsThrottled(rule, trigger, nowUtc))
                continue;

            await FireAsync(rule, trigger, nowUtc, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Test-only entry point — runs a rule's actions against a synthetic trigger without throttling.
    /// Returns the outcomes; the caller decides whether to persist.
    /// </summary>
    public async Task<IReadOnlyList<AlertActionOutcome>> RunForTestAsync(
        AlertRule rule,
        AlertTrigger trigger,
        bool persist,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(trigger);

        var nowUtc = _time.GetUtcNow();
        var evt = new AlertEvent
        {
            Id = Guid.CreateVersion7(),
            RuleId = rule.Id,
            FlowId = trigger.FlowId == Guid.Empty ? null : trigger.FlowId,
            MessageId = trigger.MessageId,
            CorrelationId = trigger.CorrelationId,
            ErrorType = trigger.ErrorType,
            ErrorDetail = trigger.ErrorDetail,
            OccurredAtUtc = nowUtc,
        };
        var outcomes = await ExecuteActionsAsync(rule, evt, cancellationToken).ConfigureAwait(false);

        if (persist)
        {
            var persisted = new AlertEvent
            {
                Id = evt.Id,
                RuleId = evt.RuleId,
                FlowId = evt.FlowId,
                MessageId = evt.MessageId,
                CorrelationId = evt.CorrelationId,
                ErrorType = evt.ErrorType,
                ErrorDetail = evt.ErrorDetail,
                OccurredAtUtc = evt.OccurredAtUtc,
                ActionOutcomes = outcomes,
            };
            await _events.AppendAsync(persisted, cancellationToken).ConfigureAwait(false);
        }
        return outcomes;
    }

    private static bool Scopes(AlertRule rule, AlertTrigger trigger)
    {
        if (rule.EnabledFlowIds is null || rule.EnabledFlowIds.Count == 0)
            return true;
        return trigger.FlowId != Guid.Empty && rule.EnabledFlowIds.Contains(trigger.FlowId);
    }

    private static bool MatchesAnyPattern(AlertRule rule, AlertTrigger trigger)
    {
        if (rule.ErrorPatterns.Count == 0)
            return true;
        foreach (var p in rule.ErrorPatterns)
        {
            if (p.ErrorType != AlertErrorType.Any && p.ErrorType != trigger.ErrorType)
                continue;
            if (!string.IsNullOrEmpty(p.Regex))
            {
                var detail = trigger.ErrorDetail ?? string.Empty;
                try
                {
                    if (!Regex.IsMatch(detail, p.Regex, RegexOptions.CultureInvariant, _regexTimeout))
                        continue;
                }
                catch (RegexMatchTimeoutException)
                {
                    continue;
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }
            return true;
        }
        return false;
    }

    private bool IsThrottled(AlertRule rule, AlertTrigger trigger, DateTimeOffset nowUtc)
    {
        var window = rule.ThrottleWindow ?? _defaultThrottleWindow;
        if (window <= TimeSpan.Zero)
            return false;

        var key = (rule.Id, trigger.FlowId, trigger.ErrorType);
        if (_lastFiredUtc.TryGetValue(key, out var last) && nowUtc - last < window)
        {
            return true;
        }
        _lastFiredUtc[key] = nowUtc;
        return false;
    }

    private async Task FireAsync(AlertRule rule, AlertTrigger trigger, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var evt = new AlertEvent
        {
            Id = Guid.CreateVersion7(),
            RuleId = rule.Id,
            FlowId = trigger.FlowId == Guid.Empty ? null : trigger.FlowId,
            MessageId = trigger.MessageId,
            CorrelationId = trigger.CorrelationId,
            ErrorType = trigger.ErrorType,
            ErrorDetail = trigger.ErrorDetail,
            OccurredAtUtc = nowUtc,
        };

        var outcomes = await ExecuteActionsAsync(rule, evt, cancellationToken).ConfigureAwait(false);

        var persisted = new AlertEvent
        {
            Id = evt.Id,
            RuleId = evt.RuleId,
            FlowId = evt.FlowId,
            MessageId = evt.MessageId,
            CorrelationId = evt.CorrelationId,
            ErrorType = evt.ErrorType,
            ErrorDetail = evt.ErrorDetail,
            OccurredAtUtc = evt.OccurredAtUtc,
            ActionOutcomes = outcomes,
        };

        try
        {
            await _events.AppendAsync(persisted, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Alert engine failed to persist event for rule {RuleId}.", rule.Id);
        }
    }

    private async Task<IReadOnlyList<AlertActionOutcome>> ExecuteActionsAsync(
        AlertRule rule,
        AlertEvent evt,
        CancellationToken cancellationToken)
    {
        var outcomes = new List<AlertActionOutcome>(rule.Actions.Count);
        foreach (var slot in rule.Actions)
        {
            var attemptedAtUtc = _time.GetUtcNow();
            var provider = _plugins.TryResolveAlertActionProvider(slot.Kind);
            if (provider is null)
            {
                outcomes.Add(new AlertActionOutcome
                {
                    Kind = slot.Kind,
                    Succeeded = false,
                    ErrorDetail = $"No alert action provider registered for kind '{slot.Kind}'.",
                    AttemptedAtUtc = attemptedAtUtc,
                });
                continue;
            }

            try
            {
                var result = await provider.ExecuteAsync(evt, rule, slot, cancellationToken).ConfigureAwait(false);
                outcomes.Add(new AlertActionOutcome
                {
                    Kind = slot.Kind,
                    Succeeded = result.Succeeded,
                    ErrorDetail = result.ErrorDetail,
                    ResponseSummary = result.ResponseSummary,
                    AttemptedAtUtc = attemptedAtUtc,
                });
            }
            catch (Exception ex)
            {
                outcomes.Add(new AlertActionOutcome
                {
                    Kind = slot.Kind,
                    Succeeded = false,
                    ErrorDetail = ex.Message,
                    AttemptedAtUtc = attemptedAtUtc,
                });
            }
        }
        return outcomes;
    }
}

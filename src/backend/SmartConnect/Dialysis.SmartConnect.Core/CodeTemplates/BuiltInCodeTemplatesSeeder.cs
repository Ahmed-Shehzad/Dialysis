using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// Idempotent first-run seeder for the built-in <see cref="CodeTemplateLibrary"/>. Writes the well-known library
/// at startup if it doesn't already exist. Templates are JS helpers that work in Jint (no Node/browser globals).
/// </summary>
public sealed class BuiltInCodeTemplatesSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BuiltInCodeTemplatesSeeder> _logger;
    /// <summary>
    /// Idempotent first-run seeder for the built-in <see cref="CodeTemplateLibrary"/>. Writes the well-known library
    /// at startup if it doesn't already exist. Templates are JS helpers that work in Jint (no Node/browser globals).
    /// </summary>
    public BuiltInCodeTemplatesSeeder(IServiceScopeFactory scopeFactory,
        ILogger<BuiltInCodeTemplatesSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    public static readonly Guid BuiltInLibraryId = Guid.Parse("00000000-0000-4000-8000-000000c0de01");
    public const string BuiltInLibraryName = "SmartConnect Built-In";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetService<ICodeTemplateLibraryRepository>();
            if (repo is null)
            {
                _logger.LogDebug("ICodeTemplateLibraryRepository not registered; skipping built-in seed.");
                return;
            }

            var existing = await repo.GetByIdAsync(BuiltInLibraryId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                _logger.LogDebug("Built-in code template library already seeded; skipping.");
                return;
            }

            var library = BuildBuiltInLibrary();
            await repo.UpsertAsync(library, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Seeded built-in code template library with {Count} templates.", library.Templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed built-in code template library; SmartConnect will continue without it.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static CodeTemplateLibrary BuildBuiltInLibrary()
    {
        var now = DateTimeOffset.UtcNow;
        var allTransformerContexts = new[]
        {
            CodeTemplateContext.SourceTransformer,
            CodeTemplateContext.DestinationTransformer,
            CodeTemplateContext.DestinationResponseTransformer,
            CodeTemplateContext.SourceFilter,
            CodeTemplateContext.DestinationFilter,
            CodeTemplateContext.ChannelPreprocessor,
            CodeTemplateContext.ChannelPostprocessor,
        };

        return new CodeTemplateLibrary
        {
            Id = BuiltInLibraryId,
            Name = BuiltInLibraryName,
            Description = "Bundled helper functions auto-linked to every new flow.",
            LinkedFlowIds = [],
            AutoLinkNewFlows = true,
            Revision = 1,
            LastModifiedUtc = now,
            Templates = new List<CodeTemplate>
            {
                BuildTemplate("formatHl7Date", FormatHl7DateBody, allTransformerContexts, position: 0, lastModified: now),
                BuildTemplate("parseHl7Date", ParseHl7DateBody, allTransformerContexts, position: 1, lastModified: now),
                BuildTemplate("escapeXml", EscapeXmlBody, allTransformerContexts, position: 2, lastModified: now),
                BuildTemplate("escapeHl7", EscapeHl7Body, allTransformerContexts, position: 3, lastModified: now),
                BuildTemplate("makeAck", MakeAckBody, [CodeTemplateContext.SourceTransformer, CodeTemplateContext.DestinationResponseTransformer], position: 4, lastModified: now),
                BuildTemplate("generateGuid", GenerateGuidBody, allTransformerContexts, position: 5, lastModified: now),
            },
        };
    }

    private static CodeTemplate BuildTemplate(
        string name,
        string code,
        IReadOnlyList<CodeTemplateContext> contexts,
        int position,
        DateTimeOffset lastModified) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            LibraryId = BuiltInLibraryId,
            Name = name,
            Code = code,
            Type = CodeTemplateType.Function,
            Contexts = contexts,
            JsDoc = ExtractLeadingJsDoc(code),
            Revision = 1,
            LastModifiedUtc = lastModified,
            Position = position,
        };

    private static string? ExtractLeadingJsDoc(string code)
    {
        var trimmed = code.TrimStart();
        if (!trimmed.StartsWith("/**", StringComparison.Ordinal))
            return null;
        var end = trimmed.IndexOf("*/", StringComparison.Ordinal);
        return end > 0 ? trimmed[..(end + 2)] : null;
    }

    // ---- Template bodies ----

    private const string FormatHl7DateBody = """
        /**
         * Format a JS Date as an HL7 v2 timestamp (YYYYMMDDhhmmss, UTC).
         * @param {Date} date
         * @return {string}
         */
        function formatHl7Date(date) {
            var d = (date instanceof Date) ? date : new Date(date);
            function pad(n) { return n < 10 ? '0' + n : '' + n; }
            return d.getUTCFullYear()
                 + pad(d.getUTCMonth() + 1)
                 + pad(d.getUTCDate())
                 + pad(d.getUTCHours())
                 + pad(d.getUTCMinutes())
                 + pad(d.getUTCSeconds());
        }
        """;

    private const string ParseHl7DateBody = """
        /**
         * Parse an HL7 v2 timestamp (YYYY[MM[DD[hh[mm[ss]]]]]) into a JS Date.
         * @param {string} s
         * @return {Date}
         */
        function parseHl7Date(s) {
            if (!s) return null;
            var t = String(s);
            var y = parseInt(t.substr(0, 4), 10) || 1970;
            var mo = (parseInt(t.substr(4, 2), 10) || 1) - 1;
            var d = parseInt(t.substr(6, 2), 10) || 1;
            var h = parseInt(t.substr(8, 2), 10) || 0;
            var mi = parseInt(t.substr(10, 2), 10) || 0;
            var se = parseInt(t.substr(12, 2), 10) || 0;
            return new Date(Date.UTC(y, mo, d, h, mi, se));
        }
        """;

    private const string EscapeXmlBody = """
        /**
         * Replace XML special characters (& < > " ') with their entity references.
         * @param {string} s
         * @return {string}
         */
        function escapeXml(s) {
            if (s === null || typeof s === 'undefined') return '';
            return String(s)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&apos;');
        }
        """;

    private const string EscapeHl7Body = """
        /**
         * Escape HL7 v2 special characters (| ^ ~ \ &) using standard \F\ \S\ \R\ \E\ \T\ sequences.
         * @param {string} s
         * @return {string}
         */
        function escapeHl7(s) {
            if (s === null || typeof s === 'undefined') return '';
            return String(s)
                .replace(/\\/g, '\\E\\')
                .replace(/\|/g, '\\F\\')
                .replace(/\^/g, '\\S\\')
                .replace(/~/g, '\\R\\')
                .replace(/&/g, '\\T\\');
        }
        """;

    private const string MakeAckBody = """
        /**
         * Build a minimal HL7 v2 ACK message.
         * @param {string} controlId - Original message control id (MSH-10).
         * @param {string} code - Acknowledgement code (AA / AE / AR).
         * @param {string} message - Optional human-readable text.
         * @return {string}
         */
        function makeAck(controlId, code, message) {
            var ts = formatHl7Date(new Date());
            var ackCode = code || 'AA';
            var msg = (message || '').replace(/[\r\n|]/g, ' ');
            return 'MSH|^~\\&|SMARTCONNECT|SMARTCONNECT|||' + ts + '||ACK|' + (controlId || '') + '|P|2.5\r' +
                   'MSA|' + ackCode + '|' + (controlId || '') + '|' + msg;
        }
        """;

    private const string GenerateGuidBody = """
        /**
         * Generate a v4-style GUID using Math.random (acceptable fallback when crypto.randomUUID is unavailable).
         * @return {string}
         */
        function generateGuid() {
            function r() { return ((Math.random() * 0x10000) | 0).toString(16).padStart(4, '0'); }
            return r() + r() + '-' + r() + '-' + ('4' + r().substr(1)) + '-' +
                   (((Math.random() * 4) | 0 + 8).toString(16) + r().substr(1)) + '-' + r() + r() + r();
        }
        """;
}

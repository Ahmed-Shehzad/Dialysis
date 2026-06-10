using Markdig;
using Stubble.Core;
using Stubble.Core.Builders;

namespace Dialysis.PDMS.Reporting.Templating;

/// <summary>
/// Binds an operator-authored Markdown+Mustache template against a flat property bag, then
/// produces plaintext suitable for the PDF renderer's paragraph blocks. Mustache is the
/// expressive subset operators learn quickly (loops, conditionals via section blocks); we
/// strip HTML out of the Markdig output so the renderer's deterministic font + style is
/// preserved end-to-end.
///
/// The binder is intentionally lightweight: it does not run scripts, does not load
/// includes, does not touch the file system. Templates are bytes-in, bytes-out.
/// </summary>
public sealed class MustacheMarkdownBinder
{
    private static readonly StubbleVisitorRenderer _stubble = new StubbleBuilder().Build();
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>Binds the Mustache placeholders and returns the rendered Markdown body (still Markdown).</summary>
    public static string BindMarkdown(string templateBody, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateBody);
        return _stubble.Render(templateBody, values);
    }

    /// <summary>
    /// Binds then renders to plain text — strips Markdown formatting so the PDF renderer
    /// owns the visual style. Use this for fields that flow into paragraph / key-value blocks.
    /// </summary>
    public string BindToPlainText(string templateBody, IReadOnlyDictionary<string, object?> values)
    {
        var bound = BindMarkdown(templateBody, values);
        return Markdown.ToPlainText(bound, _pipeline).Trim();
    }
}

using System.Text;
using Dialysis.SmartConnect.CodeTemplates;
using Jint;

namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Compiles linked Code Templates into a Jint engine before the user script runs. Templates whose
/// <see cref="CodeTemplate.Contexts"/> includes <paramref name="context"/> are prepended verbatim;
/// function declarations become globals, JSDoc comments are ignored by the engine.
/// </summary>
internal static class CodeTemplateJsBinder
{
    public async static Task PrependLinkedTemplatesAsync(
        Engine engine,
        ICodeTemplateLibraryRepository repository,
        Guid flowId,
        CodeTemplateContext context,
        CancellationToken cancellationToken)
    {
        var templates = await repository
            .GetLinkedTemplatesForFlowAsync(flowId, context, cancellationToken)
            .ConfigureAwait(false);
        if (templates.Count == 0) return;

        var sb = new StringBuilder(2048);
        foreach (var t in templates)
        {
            sb.Append("// --- code-template ").Append(t.Name).Append(" (lib=").Append(t.LibraryId).AppendLine(") ---");
            sb.AppendLine(t.Code);
            sb.AppendLine();
        }

        engine.Execute(sb.ToString());
    }
}

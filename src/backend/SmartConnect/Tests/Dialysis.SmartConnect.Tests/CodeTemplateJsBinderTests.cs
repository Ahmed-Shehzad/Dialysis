using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.VariableMaps;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Exercises <c>CodeTemplateJsBinder</c> indirectly through <see cref="JavascriptTransformStage"/> (internal binder).
/// Verifies that only context-matching templates are injected as globals before user scripts run.
/// </summary>
public sealed class CodeTemplateJsBinderTests
{
    [Fact]
    public async Task Matching_context_template_is_callable_from_user_script()
    {
        var flowId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var repo = new StubRepository(libraryId, [
            NewTemplate(libraryId, "matching", "function matching(){ return 'M'; }",
                [CodeTemplateContext.SourceTransformer]),
        ]);
        var sp = BuildServices(repo);
        var ctx = new FlowExecutionContext();
        ctx.SetCurrentStageContext(CodeTemplateContext.SourceTransformer);
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var msg = WrapMessage(flowId).WithMetadata(
            JavascriptTransformStage.ParametersMetadataKey,
            Params("matching()"));

        var result = await new JavascriptTransformStage(sp).TransformAsync(msg, CancellationToken.None);
        Assert.Equal("M", Encoding.UTF8.GetString(result.Payload.Span));
    }

    [Fact]
    public async Task Non_matching_context_template_is_not_injected()
    {
        var flowId = Guid.CreateVersion7();
        var libraryId = Guid.CreateVersion7();
        var repo = new StubRepository(libraryId, [
            NewTemplate(libraryId, "destOnly", "function destOnly(){ return 'D'; }",
                [CodeTemplateContext.DestinationTransformer]),
        ]);
        var sp = BuildServices(repo);
        var ctx = new FlowExecutionContext();
        ctx.SetCurrentStageContext(CodeTemplateContext.SourceTransformer);
        sp.GetRequiredService<IFlowExecutionContextAccessor>().Current = ctx;

        var msg = WrapMessage(flowId).WithMetadata(
            JavascriptTransformStage.ParametersMetadataKey,
            Params("typeof destOnly === 'function' ? 'present' : 'absent'"));

        var result = await new JavascriptTransformStage(sp).TransformAsync(msg, CancellationToken.None);
        Assert.Equal("absent", Encoding.UTF8.GetString(result.Payload.Span));
    }

    private static IServiceProvider BuildServices(ICodeTemplateLibraryRepository repo)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFlowExecutionContextAccessor, FlowExecutionContextAccessor>();
        services.AddSingleton<IVariableMapStore, InMemoryVariableMapStore>();
        services.AddSingleton(repo);
        return services.BuildServiceProvider();
    }

    private static CodeTemplate NewTemplate(
        Guid libraryId,
        string name,
        string code,
        IReadOnlyList<CodeTemplateContext> contexts) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            LibraryId = libraryId,
            Name = name,
            Code = code,
            Contexts = contexts,
            LastModifiedUtc = DateTimeOffset.UtcNow,
        };

    private static string Params(string script) =>
        $$$"""{"script": {{{JsonSerializer.Serialize(script)}}} }""";

    private static IntegrationMessage WrapMessage(Guid flowId) =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = flowId,
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("orig"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

    /// <summary>Tiny in-memory repo backed by a flat template list, filtered by context on read.</summary>
    private sealed class StubRepository(Guid libraryId, IReadOnlyList<CodeTemplate> templates) : ICodeTemplateLibraryRepository
    {
        public Task<IReadOnlyList<CodeTemplateLibrary>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CodeTemplateLibrary>>(
                [new CodeTemplateLibrary { Id = libraryId, Name = "stub", Templates = templates, LastModifiedUtc = DateTimeOffset.UtcNow }]);

        public Task<CodeTemplateLibrary?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<CodeTemplateLibrary?>(id == libraryId
                ? new CodeTemplateLibrary { Id = libraryId, Name = "stub", Templates = templates, LastModifiedUtc = DateTimeOffset.UtcNow }
                : null);

        public Task UpsertAsync(CodeTemplateLibrary library, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<CodeTemplate>> GetLinkedTemplatesForFlowAsync(
            Guid flowId,
            CodeTemplateContext context,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CodeTemplate>>(
                templates.Where(t => t.Contexts.Contains(context)).ToList());
    }
}

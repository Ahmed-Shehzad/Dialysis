namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// Persistence-agnostic store for <see cref="CodeTemplateLibrary"/> entries and their
/// <see cref="CodeTemplate"/> children. Linkage to flows is reconciled by
/// <c>CodeTemplateLinkageService</c>; implementations of this interface only own the library aggregate.
/// </summary>
public interface ICodeTemplateLibraryRepository
{
    Task<IReadOnlyList<CodeTemplateLibrary>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CodeTemplateLibrary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Insert or replace a full library aggregate (templates included).</summary>
    Task UpsertAsync(CodeTemplateLibrary library, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hot-path: returns every <see cref="CodeTemplate"/> applicable to <paramref name="flowId"/> running in
    /// <paramref name="context"/>. Combines (a) libraries whose <see cref="CodeTemplateLibrary.LinkedFlowIds"/>
    /// includes <paramref name="flowId"/> and (b) libraries referenced by the flow's
    /// <c>IntegrationFlowPipelineDefinition.LinkedLibraryIds</c>. Templates are then filtered to those whose
    /// <see cref="CodeTemplate.Contexts"/> contains <paramref name="context"/>.
    /// </summary>
    Task<IReadOnlyList<CodeTemplate>> GetLinkedTemplatesForFlowAsync(
        Guid flowId,
        CodeTemplateContext context,
        CancellationToken cancellationToken = default);
}

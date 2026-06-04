namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Closure-typed dispatcher for one command kind. Built once at registration time so the hot
/// consumer path never calls reflection; the closure captures the concrete <c>TCommand</c> and
/// <c>TResult</c> types and serializes the result back to JSON. Fields:
/// <list type="bullet">
///   <item><c>CommandTypeKey</c> — wire discriminator (assembly-qualified name of <c>CommandType</c>).</item>
///   <item><c>CommandType</c> — CLR type implementing <c>ICommand&lt;TResult&gt;</c>.</item>
///   <item><c>ResultType</c> — CLR type of the handler's return value.</item>
///   <item><c>ModuleSlug</c> — owner module slug.</item>
///   <item><c>RequiredPermission</c> — permission gate enforced by the status endpoint.</item>
///   <item><c>Deserialize</c> — JSON → command instance.</item>
///   <item><c>Dispatch</c> — invokes the registered <c>ICommandHandler</c> through the CQRS gateway and serializes the result.</item>
/// </list>
/// </summary>
public sealed record DurableCommandRegistration
{
    /// <summary>
    /// Closure-typed dispatcher for one command kind. Built once at registration time so the hot
    /// consumer path never calls reflection; the closure captures the concrete <c>TCommand</c> and
    /// <c>TResult</c> types and serializes the result back to JSON. Fields:
    /// <list type="bullet">
    ///   <item><c>CommandTypeKey</c> — wire discriminator (assembly-qualified name of <c>CommandType</c>).</item>
    ///   <item><c>CommandType</c> — CLR type implementing <c>ICommand&lt;TResult&gt;</c>.</item>
    ///   <item><c>ResultType</c> — CLR type of the handler's return value.</item>
    ///   <item><c>ModuleSlug</c> — owner module slug.</item>
    ///   <item><c>RequiredPermission</c> — permission gate enforced by the status endpoint.</item>
    ///   <item><c>Deserialize</c> — JSON → command instance.</item>
    ///   <item><c>Dispatch</c> — invokes the registered <c>ICommandHandler</c> through the CQRS gateway and serializes the result.</item>
    /// </list>
    /// </summary>
    public DurableCommandRegistration(string CommandTypeKey,
        Type CommandType,
        Type ResultType,
        string ModuleSlug,
        string? RequiredPermission,
        Func<string, object> Deserialize,
        Func<object, IServiceProvider, CancellationToken, Task<string?>> Dispatch)
    {
        this.CommandTypeKey = CommandTypeKey;
        this.CommandType = CommandType;
        this.ResultType = ResultType;
        this.ModuleSlug = ModuleSlug;
        this.RequiredPermission = RequiredPermission;
        this.Deserialize = Deserialize;
        this.Dispatch = Dispatch;
    }
    public string CommandTypeKey { get; init; }
    public Type CommandType { get; init; }
    public Type ResultType { get; init; }
    public string ModuleSlug { get; init; }
    public string? RequiredPermission { get; init; }
    public Func<string, object> Deserialize { get; init; }
    public Func<object, IServiceProvider, CancellationToken, Task<string?>> Dispatch { get; init; }
    public void Deconstruct(out string CommandTypeKey, out Type CommandType, out Type ResultType, out string ModuleSlug, out string? RequiredPermission, out Func<string, object> Deserialize, out Func<object, IServiceProvider, CancellationToken, Task<string?>> Dispatch)
    {
        CommandTypeKey = this.CommandTypeKey;
        CommandType = this.CommandType;
        ResultType = this.ResultType;
        ModuleSlug = this.ModuleSlug;
        RequiredPermission = this.RequiredPermission;
        Deserialize = this.Deserialize;
        Dispatch = this.Dispatch;
    }
}

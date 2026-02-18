namespace Transponder.Abstractions;

/// <summary>
/// Provides message-scoped resources for Transponder operations.
/// </summary>
public interface ITransponderMessageScopeProvider
{
    IDisposable? BeginSend(TransponderMessageContext context);

    IDisposable? BeginPublish(TransponderMessageContext context);

    IDisposable? BeginConsume(TransponderMessageContext context);
}

namespace Verifier;

public sealed class ValidationContext<TRequest>
{
    public TRequest Request { get; }
    public CancellationToken CancellationToken { get; }

    // Optional: stash extra data (user id, correlation id, etc.)
    public IDictionary<string, object> Items { get; }

    public ValidationContext(
        TRequest request,
        CancellationToken cancellationToken = default,
        IDictionary<string, object>? items = null)
    {
        Request = request;
        CancellationToken = cancellationToken;
        Items = items ?? new Dictionary<string, object>();
    }
}
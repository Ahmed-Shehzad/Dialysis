namespace Verifier.Exceptions;

public sealed class ValidationException : Exception
{
    public IReadOnlyList<ValidationFailure> Errors { get; }

    public ValidationException(List<ValidationFailure> failures) : base(BuildMessage(failures))
    {
        Errors = failures.AsReadOnly();
    }

    private static string BuildMessage(List<ValidationFailure> failures) => failures.Count == 0 ? string.Empty : "Validation failed: " + string.Join("; ", failures);
}

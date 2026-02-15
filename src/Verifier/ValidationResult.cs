namespace Verifier;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationFailure> Errors { get; } = [];

    public void AddError(ValidationFailure errorMessage) => Errors.Add(errorMessage);
}

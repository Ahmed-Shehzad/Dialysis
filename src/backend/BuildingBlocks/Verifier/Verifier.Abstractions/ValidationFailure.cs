namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// A single validation error for a member path.
/// </summary>
public sealed class ValidationFailure
{
    public ValidationFailure(string propertyName, string errorMessage, object? attemptedValue = null, string? errorCode = null)
    {
        PropertyName = propertyName ?? string.Empty;
        ErrorMessage = errorMessage ?? string.Empty;
        AttemptedValue = attemptedValue;
        ErrorCode = errorCode;
    }

    public string PropertyName { get; }

    public string ErrorMessage { get; }

    public object? AttemptedValue { get; }

    public string? ErrorCode { get; }
}

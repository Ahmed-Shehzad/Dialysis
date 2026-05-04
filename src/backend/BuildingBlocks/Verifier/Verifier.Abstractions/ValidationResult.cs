namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Outcome of validating a <typeparamref name="T"/>: either the instance or <see cref="ValidationErrors"/>.
/// </summary>
public readonly struct ValidationResult<T>
{
    private readonly bool _success;
    private readonly T? _value;
    private readonly ValidationErrors? _errors;

    private ValidationResult(bool success, T? value, ValidationErrors? errors)
    {
        _success = success;
        _value = value;
        _errors = errors;
    }

    public bool IsSuccess => _success;

    public bool IsFailure => !_success;

    public static ValidationResult<T> Ok(T value) => new(true, value, null);

    public static ValidationResult<T> Fail(ValidationErrors errors) => new(false, default, errors);

    public T Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("No value on a failed validation result.");

    public ValidationErrors Error =>
        IsFailure ? _errors! : throw new InvalidOperationException("No errors on a successful validation result.");

    public TMatch Match<TMatch>(Func<T, TMatch> onSuccess, Func<ValidationErrors, TMatch> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_errors!);

    public void Match(Action<T> onSuccess, Action<ValidationErrors> onFailure)
    {
        if (IsSuccess)
            onSuccess(_value!);
        else
            onFailure(_errors!);
    }

    public T GetValueOrDefault(T defaultValue = default!) =>
        IsSuccess ? _value! : defaultValue;
}

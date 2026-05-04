namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Context passed to validators; mirrors the shape used by common validation libraries.
/// </summary>
public sealed class ValidationContext<T>
{
    public ValidationContext(T instanceToValidate)
    {
        InstanceToValidate = instanceToValidate;
    }

    public T InstanceToValidate { get; }
}

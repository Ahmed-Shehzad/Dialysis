namespace Dialysis.SharedKernel.Exceptions;

/// <summary>
/// Thrown when attempting to create a patient with a logical ID that already exists for the tenant.
/// </summary>
public sealed class PatientAlreadyExistsException : InvalidOperationException
{
    public PatientAlreadyExistsException(string tenantId, string logicalId)
        : base($"Patient with logical ID '{logicalId}' already exists for tenant '{tenantId}'.")
    {
    }
}

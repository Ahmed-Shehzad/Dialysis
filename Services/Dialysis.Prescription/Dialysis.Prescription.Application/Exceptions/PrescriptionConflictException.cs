namespace Dialysis.Prescription.Application.Exceptions;

/// <summary>
/// Thrown when a prescription with the same OrderId (and tenant) already exists
/// and the conflict policy is Reject.
/// </summary>
public sealed class PrescriptionConflictException : InvalidOperationException
{
    public string OrderId { get; }

    public PrescriptionConflictException(string orderId)
        : base($"Prescription already exists for OrderId '{orderId}'.")
    {
        OrderId = orderId;
    }

    public PrescriptionConflictException(string orderId, string message) : base(message)
    {
        OrderId = orderId;
    }
}

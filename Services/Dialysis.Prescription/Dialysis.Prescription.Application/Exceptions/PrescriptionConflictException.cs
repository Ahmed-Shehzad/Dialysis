namespace Dialysis.Prescription.Application.Exceptions;

/// <summary>
/// Thrown when a prescription with the same OrderId (and tenant) already exists
/// and the conflict policy is Reject or Callback.
/// When CallbackPolicy is used, CallbackPhone is set for 409 response.
/// </summary>
public sealed class PrescriptionConflictException : InvalidOperationException
{
    public string OrderId { get; }
    public string? CallbackPhone { get; }

    public PrescriptionConflictException(string orderId)
        : base($"Prescription already exists for OrderId '{orderId}'.")
    {
        OrderId = orderId;
        CallbackPhone = null;
    }

    public PrescriptionConflictException(string orderId, string? callbackPhone)
        : base($"Prescription already exists for OrderId '{orderId}'. Contact ordering provider.")
    {
        OrderId = orderId;
        CallbackPhone = callbackPhone;
    }
}

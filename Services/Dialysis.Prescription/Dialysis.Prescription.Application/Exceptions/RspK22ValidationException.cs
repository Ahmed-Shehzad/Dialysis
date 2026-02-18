namespace Dialysis.Prescription.Application.Exceptions;

/// <summary>
/// Thrown when RSP^K22 response validation fails (MSA error, QPD mismatch, etc.).
/// </summary>
public sealed class RspK22ValidationException : Exception
{
    public string ErrorCode { get; }
    public string? Detail { get; }

    public RspK22ValidationException(string errorCode, string message, string? detail = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }
}

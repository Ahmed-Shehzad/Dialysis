namespace Dialysis.Prescription.Application.Features.IngestRspK22Message;

/// <summary>
/// How to handle ingestion when a prescription already exists for the same OrderId and tenant.
/// </summary>
public enum PrescriptionConflictPolicy
{
    /// <summary>Reject ingestion and throw.</summary>
    Reject,

    /// <summary>Replace existing prescription with the new one.</summary>
    Replace,

    /// <summary>Ignore duplicate; return success without persisting.</summary>
    Ignore,

    /// <summary>Return 409 with callback info so client can contact ordering provider.</summary>
    Callback,

    /// <summary>Merge: add only settings whose code is not already present.</summary>
    Partial
}

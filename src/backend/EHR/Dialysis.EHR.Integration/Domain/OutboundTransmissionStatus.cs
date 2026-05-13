namespace Dialysis.EHR.Integration.Domain;

public enum OutboundTransmissionStatus
{
    Queued = 1,
    Sent = 2,
    Acknowledged = 3,
    Rejected = 4,
    Failed = 5,
}

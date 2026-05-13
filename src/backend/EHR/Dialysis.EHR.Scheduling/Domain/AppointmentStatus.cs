namespace Dialysis.EHR.Scheduling.Domain;

public enum AppointmentStatus
{
    Scheduled = 1,
    CheckedIn = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6,
}

namespace Dialysis.SmartConnect.Scheduling;

/// <summary>
/// Stateless schedule evaluator. Given a reference instant, returns the next time the schedule should fire,
/// or <c>null</c> if there is no future occurrence (e.g. a time schedule whose configured times have all elapsed
/// for the day and that does not roll over).
/// </summary>
public interface ISchedule
{
    DateTimeOffset? NextOccurrence(DateTimeOffset after);
}

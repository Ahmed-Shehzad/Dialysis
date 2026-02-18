using Verifier;

namespace Dialysis.Alarm.Application.Features.RecordAlarm;

public sealed class RecordAlarmCommandValidator : AbstractValidator<RecordAlarmCommand>
{
    public RecordAlarmCommandValidator()
    {
        _ = RuleFor(x => x.Alarm)
            .NotNull("Alarm is required.");

        _ = RuleFor(x => x.Alarm.EventPhase)
            .NotEmpty("EventPhase is required.");

        _ = RuleFor(x => x.Alarm.AlarmState)
            .NotEmpty("AlarmState is required.");
    }
}

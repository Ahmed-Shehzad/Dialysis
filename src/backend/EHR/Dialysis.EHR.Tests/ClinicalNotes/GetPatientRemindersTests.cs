using Dialysis.EHR.ClinicalNotes.Features.GetPatientReminders;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.ClinicalNotes;

public sealed class GetPatientRemindersTests
{
    [Fact]
    public async Task Maps_Each_Gap_To_A_Patient_Reminder_Async()
    {
        var evaluator = new FakeEvaluator(
            [new QualityGap("MIPS-001", "HbA1c", "No result for 4548-4 in the last 12 months.")]);
        var handler = new GetPatientRemindersQueryHandler(evaluator);

        var reminders = await handler.HandleAsync(new GetPatientRemindersQuery(Guid.NewGuid()), CancellationToken.None);

        var reminder = reminders.ShouldHaveSingleItem();
        reminder.Title.ShouldBe("HbA1c");
        reminder.WhatToDo.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task No_Gaps_Means_No_Reminders_Async()
    {
        var handler = new GetPatientRemindersQueryHandler(new FakeEvaluator([]));
        var reminders = await handler.HandleAsync(new GetPatientRemindersQuery(Guid.NewGuid()), CancellationToken.None);
        reminders.ShouldBeEmpty();
    }

    private sealed class FakeEvaluator : IQualityMeasureEvaluator
    {
        private readonly IReadOnlyList<QualityGap> _gaps;
        public FakeEvaluator(IReadOnlyList<QualityGap> gaps) => _gaps = gaps;
        public Task<IReadOnlyList<QualityGap>> EvaluateAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_gaps);
    }
}

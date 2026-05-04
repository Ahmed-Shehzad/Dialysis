namespace Dialysis.HIS.Scheduling.Features.ListSchedulingResources;

public sealed record SchedulingResourceDto(Guid Id, string KindCode, string DisplayName, bool IsBookable);

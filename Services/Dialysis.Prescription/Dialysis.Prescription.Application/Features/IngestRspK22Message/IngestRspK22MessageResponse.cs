namespace Dialysis.Prescription.Application.Features.IngestRspK22Message;

public sealed record IngestRspK22MessageResponse(string OrderId, string PatientMrn, int SettingsCount, bool Success);

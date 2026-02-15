namespace Dialysis.Prediction.Services;

public interface IVitalHistoryCache
{
    void Append(string patientId, VitalSnapshot vital);
    IReadOnlyList<VitalSnapshot> GetRecent(string patientId, int maxCount = 20, TimeSpan? maxAge = null);
}

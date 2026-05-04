using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents;

namespace Dialysis.HIS.PatientFlow.Domain;

public sealed class Referral : AggregateRoot<Guid>
{
    public Referral()
    {
    }

    public Referral(Guid id)
        : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public string ReferralTypeCode { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static Referral Create(Guid id, Guid patientId, string referralTypeCode, DateTime utcNow, string? actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referralTypeCode);
        var r = new Referral(id)
        {
            PatientId = patientId,
            ReferralTypeCode = referralTypeCode,
            CreatedAtUtc = utcNow,
        };
        r.RecordCreation(utcNow, actorId);
        r.RaiseIntegrationEvent(new ReferralCreatedIntegrationEvent(id, patientId, referralTypeCode, utcNow));
        return r;
    }
}

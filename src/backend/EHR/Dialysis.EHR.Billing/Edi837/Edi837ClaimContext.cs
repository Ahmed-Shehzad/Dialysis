using Dialysis.EHR.Billing.Domain;

namespace Dialysis.EHR.Billing.Edi837;

/// <summary>
/// Aggregated context the <see cref="Edi837PClaimWriter"/> consumes. The composition root
/// builds this from the persistence repositories — pulling the claim, its charges, the
/// subscriber demographics, the billing-provider profile, the payer reference, and the
/// envelope's control numbers — then hands the bundle to the writer. Keeping the writer
/// free of any IO means the same writer covers production and test.
/// </summary>
public sealed record Edi837ClaimContext
{
    /// <summary>
    /// Aggregated context the <see cref="Edi837PClaimWriter"/> consumes. The composition root
    /// builds this from the persistence repositories — pulling the claim, its charges, the
    /// subscriber demographics, the billing-provider profile, the payer reference, and the
    /// envelope's control numbers — then hands the bundle to the writer. Keeping the writer
    /// free of any IO means the same writer covers production and test.
    /// </summary>
    public Edi837ClaimContext(Claim Claim,
        IReadOnlyList<Charge> Charges,
        DateTime GeneratedAtUtc,
        long InterchangeControlNumber,
        long GroupControlNumber,
        int TransactionControlNumber,
        string SubmitterId,
        string SubmitterName,
        string SubmitterContactName,
        string SubmitterContactPhone,
        string ReceiverId,
        string ReceiverName,
        string BillingProviderName,
        string BillingProviderNpi,
        string BillingProviderTaxId,
        string BillingProviderTaxonomyCode,
        BillingAddress BillingProviderAddress,
        SubscriberContext Subscriber,
        string SubscriberGroupNumber,
        string PayerName,
        DateTime ServicePeriodStartUtc,
        DateTime ServicePeriodEndUtc,
        IReadOnlyList<string> DiagnosisCodes,
        string PlaceOfServiceCode = "11")
    {
        this.Claim = Claim;
        this.Charges = Charges;
        this.GeneratedAtUtc = GeneratedAtUtc;
        this.InterchangeControlNumber = InterchangeControlNumber;
        this.GroupControlNumber = GroupControlNumber;
        this.TransactionControlNumber = TransactionControlNumber;
        this.SubmitterId = SubmitterId;
        this.SubmitterName = SubmitterName;
        this.SubmitterContactName = SubmitterContactName;
        this.SubmitterContactPhone = SubmitterContactPhone;
        this.ReceiverId = ReceiverId;
        this.ReceiverName = ReceiverName;
        this.BillingProviderName = BillingProviderName;
        this.BillingProviderNpi = BillingProviderNpi;
        this.BillingProviderTaxId = BillingProviderTaxId;
        this.BillingProviderTaxonomyCode = BillingProviderTaxonomyCode;
        this.BillingProviderAddress = BillingProviderAddress;
        this.Subscriber = Subscriber;
        this.SubscriberGroupNumber = SubscriberGroupNumber;
        this.PayerName = PayerName;
        this.ServicePeriodStartUtc = ServicePeriodStartUtc;
        this.ServicePeriodEndUtc = ServicePeriodEndUtc;
        this.DiagnosisCodes = DiagnosisCodes;
        this.PlaceOfServiceCode = PlaceOfServiceCode;
    }
    public Claim Claim { get; init; }
    public IReadOnlyList<Charge> Charges { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public long InterchangeControlNumber { get; init; }
    public long GroupControlNumber { get; init; }
    public int TransactionControlNumber { get; init; }
    public string SubmitterId { get; init; }
    public string SubmitterName { get; init; }
    public string SubmitterContactName { get; init; }
    public string SubmitterContactPhone { get; init; }
    public string ReceiverId { get; init; }
    public string ReceiverName { get; init; }
    public string BillingProviderName { get; init; }
    public string BillingProviderNpi { get; init; }
    public string BillingProviderTaxId { get; init; }
    public string BillingProviderTaxonomyCode { get; init; }
    public BillingAddress BillingProviderAddress { get; init; }
    public SubscriberContext Subscriber { get; init; }
    public string SubscriberGroupNumber { get; init; }
    public string PayerName { get; init; }
    public DateTime ServicePeriodStartUtc { get; init; }
    public DateTime ServicePeriodEndUtc { get; init; }
    public IReadOnlyList<string> DiagnosisCodes { get; init; }
    public string PlaceOfServiceCode { get; init; }
    public void Deconstruct(out Claim Claim, out IReadOnlyList<Charge> Charges, out DateTime GeneratedAtUtc, out long InterchangeControlNumber, out long GroupControlNumber, out int TransactionControlNumber, out string SubmitterId, out string SubmitterName, out string SubmitterContactName, out string SubmitterContactPhone, out string ReceiverId, out string ReceiverName, out string BillingProviderName, out string BillingProviderNpi, out string BillingProviderTaxId, out string BillingProviderTaxonomyCode, out BillingAddress BillingProviderAddress, out SubscriberContext Subscriber, out string SubscriberGroupNumber, out string PayerName, out DateTime ServicePeriodStartUtc, out DateTime ServicePeriodEndUtc, out IReadOnlyList<string> DiagnosisCodes, out string PlaceOfServiceCode)
    {
        Claim = this.Claim;
        Charges = this.Charges;
        GeneratedAtUtc = this.GeneratedAtUtc;
        InterchangeControlNumber = this.InterchangeControlNumber;
        GroupControlNumber = this.GroupControlNumber;
        TransactionControlNumber = this.TransactionControlNumber;
        SubmitterId = this.SubmitterId;
        SubmitterName = this.SubmitterName;
        SubmitterContactName = this.SubmitterContactName;
        SubmitterContactPhone = this.SubmitterContactPhone;
        ReceiverId = this.ReceiverId;
        ReceiverName = this.ReceiverName;
        BillingProviderName = this.BillingProviderName;
        BillingProviderNpi = this.BillingProviderNpi;
        BillingProviderTaxId = this.BillingProviderTaxId;
        BillingProviderTaxonomyCode = this.BillingProviderTaxonomyCode;
        BillingProviderAddress = this.BillingProviderAddress;
        Subscriber = this.Subscriber;
        SubscriberGroupNumber = this.SubscriberGroupNumber;
        PayerName = this.PayerName;
        ServicePeriodStartUtc = this.ServicePeriodStartUtc;
        ServicePeriodEndUtc = this.ServicePeriodEndUtc;
        DiagnosisCodes = this.DiagnosisCodes;
        PlaceOfServiceCode = this.PlaceOfServiceCode;
    }
}

public sealed record BillingAddress
{
    public BillingAddress(string Line1,
        string City,
        string StateOrProvince,
        string PostalCode)
    {
        this.Line1 = Line1;
        this.City = City;
        this.StateOrProvince = StateOrProvince;
        this.PostalCode = PostalCode;
    }
    public string Line1 { get; init; }
    public string City { get; init; }
    public string StateOrProvince { get; init; }
    public string PostalCode { get; init; }
    public void Deconstruct(out string Line1, out string City, out string StateOrProvince, out string PostalCode)
    {
        Line1 = this.Line1;
        City = this.City;
        StateOrProvince = this.StateOrProvince;
        PostalCode = this.PostalCode;
    }
}

public sealed record SubscriberContext
{
    public SubscriberContext(string FirstName,
        string LastName,
        string MemberId,
        BillingAddress Address,
        DateTime DateOfBirthUtc,
        string GenderCode)
    {
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.MemberId = MemberId;
        this.Address = Address;
        this.DateOfBirthUtc = DateOfBirthUtc;
        this.GenderCode = GenderCode;
    }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string MemberId { get; init; }
    public BillingAddress Address { get; init; }
    public DateTime DateOfBirthUtc { get; init; }
    public string GenderCode { get; init; }
    public void Deconstruct(out string FirstName, out string LastName, out string MemberId, out BillingAddress Address, out DateTime DateOfBirthUtc, out string GenderCode)
    {
        FirstName = this.FirstName;
        LastName = this.LastName;
        MemberId = this.MemberId;
        Address = this.Address;
        DateOfBirthUtc = this.DateOfBirthUtc;
        GenderCode = this.GenderCode;
    }
}

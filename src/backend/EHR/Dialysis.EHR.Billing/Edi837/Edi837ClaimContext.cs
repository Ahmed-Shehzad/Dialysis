using Dialysis.EHR.Billing.Domain;

namespace Dialysis.EHR.Billing.Edi837;

/// <summary>
/// Aggregated context the <see cref="Edi837PClaimWriter"/> consumes. The composition root
/// builds this from the persistence repositories — pulling the claim, its charges, the
/// subscriber demographics, the billing-provider profile, the payer reference, and the
/// envelope's control numbers — then hands the bundle to the writer. Keeping the writer
/// free of any IO means the same writer covers production and test.
/// </summary>
public sealed record Edi837ClaimContext(
    Claim Claim,
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
    string PlaceOfServiceCode = "11");

public sealed record BillingAddress(
    string Line1,
    string City,
    string StateOrProvince,
    string PostalCode);

public sealed record SubscriberContext(
    string FirstName,
    string LastName,
    string MemberId,
    BillingAddress Address,
    DateTime DateOfBirthUtc,
    string GenderCode);

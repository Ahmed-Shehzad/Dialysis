namespace Dialysis.Fhir.Api.Subscriptions;

/// <summary>
/// Store for FHIR Subscription resources.
/// </summary>
public interface ISubscriptionStore
{
    void Add(string id, Hl7.Fhir.Model.Subscription subscription);
    bool TryGet(string id, out Hl7.Fhir.Model.Subscription? subscription);
    bool Remove(string id);
    IReadOnlyList<Hl7.Fhir.Model.Subscription> GetActiveRestHookSubscriptions();
}

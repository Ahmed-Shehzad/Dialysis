namespace Transponder;

internal sealed class SagaRegistration
{
    public SagaRegistration(IReadOnlyList<SagaMessageRegistration> registrations)
    {
        Registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
    }

    public IReadOnlyList<SagaMessageRegistration> Registrations { get; }
}

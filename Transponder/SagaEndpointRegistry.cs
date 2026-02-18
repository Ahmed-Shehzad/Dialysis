namespace Transponder;

internal sealed class SagaEndpointRegistry
{
    private readonly Dictionary<string, Dictionary<string, List<SagaMessageRegistration>>> _registrations;

    public SagaEndpointRegistry(IEnumerable<SagaRegistration> registrations)
    {
        _registrations = new Dictionary<string, Dictionary<string, List<SagaMessageRegistration>>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (SagaRegistration registration in registrations)
            foreach (SagaMessageRegistration message in registration.Registrations)
            {
                string addressKey = message.InputAddress.ToString();
                if (!_registrations.TryGetValue(addressKey, out Dictionary<string, List<SagaMessageRegistration>>? byMessageType))
                {
                    byMessageType = new Dictionary<string, List<SagaMessageRegistration>>(StringComparer.OrdinalIgnoreCase);
                    _registrations[addressKey] = byMessageType;
                }

                if (!byMessageType.TryGetValue(message.MessageTypeName, out List<SagaMessageRegistration>? list))
                {
                    list = [];
                    byMessageType[message.MessageTypeName] = list;
                }

                list.Add(message);
            }
    }

    public IReadOnlyCollection<Uri> GetInputAddresses()
        => [.. _registrations.Keys.Select(static key => new Uri(key, UriKind.RelativeOrAbsolute))];

    public bool TryGetHandlers(
        Uri inputAddress,
        string messageTypeName,
        out IReadOnlyList<SagaMessageRegistration> registrations)
    {
        registrations = Array.Empty<SagaMessageRegistration>();

        string addressKey = inputAddress.ToString();
        if (!_registrations.TryGetValue(addressKey, out Dictionary<string, List<SagaMessageRegistration>>? byMessageType)) return false;

        if (!byMessageType.TryGetValue(messageTypeName, out List<SagaMessageRegistration>? list)) return false;

        registrations = list;
        return true;
    }
}

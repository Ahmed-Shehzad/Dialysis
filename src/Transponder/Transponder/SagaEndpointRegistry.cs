namespace Transponder;

internal sealed class SagaEndpointRegistry
{
    private readonly Dictionary<string, Dictionary<string, List<SagaMessageRegistration>>> _registrations;

    public SagaEndpointRegistry(IEnumerable<SagaRegistration> registrations)
    {
        _registrations = new Dictionary<string, Dictionary<string, List<SagaMessageRegistration>>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var registration in registrations)
            foreach (var message in registration.Registrations)
            {
                var addressKey = message.InputAddress.ToString();
                if (!_registrations.TryGetValue(addressKey, out var byMessageType))
                {
                    byMessageType = new Dictionary<string, List<SagaMessageRegistration>>(StringComparer.OrdinalIgnoreCase);
                    _registrations[addressKey] = byMessageType;
                }

                if (!byMessageType.TryGetValue(message.MessageTypeName, out var list))
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

        var addressKey = inputAddress.ToString();
        if (!_registrations.TryGetValue(addressKey, out var byMessageType)) return false;

        if (!byMessageType.TryGetValue(messageTypeName, out var list)) return false;

        registrations = list;
        return true;
    }
}

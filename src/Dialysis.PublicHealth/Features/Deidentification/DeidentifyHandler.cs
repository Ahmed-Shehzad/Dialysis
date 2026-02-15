using System.Text;
using Dialysis.PublicHealth.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Intercessor.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.PublicHealth.Features.Deidentification;

public sealed class DeidentifyHandler : ICommandHandler<DeidentifyCommand>
{
    private readonly IDeidentificationPipeline _pipeline;
    private readonly FhirJsonDeserializer _deserializer = new();
    private readonly FhirJsonSerializer _serializer = new();

    public DeidentifyHandler(IDeidentificationPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task HandleAsync(DeidentifyCommand request, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(request.Input);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var resource = _deserializer.Deserialize<Resource>(json);

        var deidentified = await _pipeline.DeidentifyAsync(resource, request.Options, cancellationToken);
        var outputJson = _serializer.SerializeToString(deidentified);
        await request.Output.WriteAsync(Encoding.UTF8.GetBytes(outputJson), cancellationToken);
    }
}

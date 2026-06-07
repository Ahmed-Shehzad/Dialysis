using System.Text;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

public sealed class FhirContentResult : IActionResult
{
    private readonly Base _resource;
    private readonly int _statusCode;
    public FhirContentResult(Base resource, int statusCode)
    {
        _resource = resource;
        _statusCode = statusCode;
    }
    public async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var response = context.HttpContext.Response;
        response.StatusCode = _statusCode;
        response.ContentType = FhirContentTypes.Json + "; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(FhirJsonSerializerProvider.Serialize(_resource));
        await response.Body.WriteAsync(bytes, context.HttpContext.RequestAborted).ConfigureAwait(false);
    }
}

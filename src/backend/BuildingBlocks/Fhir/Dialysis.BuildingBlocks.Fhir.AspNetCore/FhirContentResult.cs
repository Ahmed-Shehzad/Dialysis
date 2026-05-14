using System.Text;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore;

public sealed class FhirContentResult(Base resource, int statusCode, FhirJsonSerializerProvider serializer) : IActionResult
{
    public async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var response = context.HttpContext.Response;
        response.StatusCode = statusCode;
        response.ContentType = FhirContentTypes.Json + "; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(resource));
        await response.Body.WriteAsync(bytes, context.HttpContext.RequestAborted).ConfigureAwait(false);
    }
}

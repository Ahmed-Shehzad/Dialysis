using Asp.Versioning;
using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PublicHealth.Features.Deidentification;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/deidentify")]
[Authorize(Policy = "Write")]
public sealed class DeidentificationController : ControllerBase
{
    private readonly ISender _sender;

    public DeidentificationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>De-identify a FHIR resource (JSON body). Options via query: level=Basic|SafeHarbor|ExpertDetermination.</summary>
    [HttpPost]
    public async Task<IActionResult> Deidentify(
        [FromQuery] string? level = null,
        [FromQuery] bool? removeDirectIdentifiers = null,
        [FromQuery] bool? generalizeDates = null,
        [FromQuery] bool? removeFreeText = null,
        [FromQuery] bool? generalizeAgesOver89 = null,
        CancellationToken cancellationToken = default)
    {
        var lvl = Enum.TryParse<DeidentificationLevel>(level, ignoreCase: true, out var parsed) ? parsed : DeidentificationLevel.Basic;
        var options = new DeidentificationOptions
        {
            Level = lvl,
            RemoveDirectIdentifiers = removeDirectIdentifiers ?? true,
            GeneralizeDates = generalizeDates ?? true,
            RemoveFreeText = removeFreeText ?? true,
            GeneralizeAgesOver89 = generalizeAgesOver89 ?? (lvl >= DeidentificationLevel.SafeHarbor)
        };

        using var inputStream = new MemoryStream();
        await Request.Body.CopyToAsync(inputStream, cancellationToken);
        inputStream.Position = 0;

        using var outputStream = new MemoryStream();
        await _sender.SendAsync(new DeidentifyCommand(inputStream, outputStream, options), cancellationToken);
        outputStream.Position = 0;

        return File(outputStream.ToArray(), "application/fhir+json");
    }
}

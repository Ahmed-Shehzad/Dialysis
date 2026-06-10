using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.Api.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Features.ListMessageThreads;
using Dialysis.EHR.PatientPortal.Features.ListThreadMessages;
using Dialysis.EHR.PatientPortal.Features.MarkMessageRead;
using Dialysis.EHR.PatientPortal.Features.ReplySecureMessage;
using Dialysis.EHR.PatientPortal.Features.SendSecureMessage;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Two-way secure messaging between a patient and their care team. Patient-scoped routes are gated by
/// the caller's own patient identity claim (<see cref="EhrPatientAccess"/>); provider routes are gated
/// by permission only. The patient sends and reads under <c>patients/{patientId}</c>; the care team
/// replies and reviews under <c>provider/patients/{patientId}</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portal/messages")]
public sealed class PortalMessagesController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    private readonly EhrPortalAccess _portalAccess;

    public PortalMessagesController(ICqrsGateway gateway, EhrPortalAccess portalAccess)
    {
        _gateway = gateway;
        _portalAccess = portalAccess;
    }

    private bool IsSelf(Guid patientId) => _portalAccess.CanActAs(User, patientId);

    /// <summary>Patient sends a secure message to their care team (starts or continues a thread).</summary>
    [HttpPost("patients/{patientId:guid}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendAsync(
        Guid patientId, [FromBody] SendMessageRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!IsSelf(patientId))
            return Forbid();

        var id = await _gateway.SendCommandAsync<SendSecureMessageCommand, Guid>(
            new SendSecureMessageCommand(
                patientId, body.ThreadId, body.TargetProviderId,
                SecureMessageDirection.PatientToProvider, body.Subject, body.Body),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/portal/messages/patients/{patientId}", new { id });
    }

    /// <summary>Patient's own message threads (inbox), newest activity first.</summary>
    [HttpGet("patients/{patientId:guid}/threads")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageThreadView>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListMyThreadsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (!IsSelf(patientId))
            return Forbid();
        var threads = await _gateway.SendQueryAsync<ListMessageThreadsForPatientQuery, IReadOnlyList<MessageThreadView>>(
            new ListMessageThreadsForPatientQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(threads);
    }

    /// <summary>Messages in one of the patient's own threads.</summary>
    [HttpGet("patients/{patientId:guid}/threads/{threadId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<SecureMessageView>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyThreadAsync(Guid patientId, Guid threadId, CancellationToken cancellationToken)
    {
        if (!IsSelf(patientId))
            return Forbid();
        var messages = await _gateway.SendQueryAsync<ListThreadMessagesQuery, IReadOnlyList<SecureMessageView>>(
            new ListThreadMessagesQuery(threadId), cancellationToken).ConfigureAwait(false);
        // Guard against probing another patient's thread by id.
        if (messages.Any(m => m.PatientId != patientId))
            return Forbid();
        return Ok(messages);
    }

    /// <summary>Patient marks a care-team message read (clears the unread badge).</summary>
    [HttpPost("patients/{patientId:guid}/messages/{messageId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkReadAsync(Guid patientId, Guid messageId, CancellationToken cancellationToken)
    {
        if (!IsSelf(patientId))
            return Forbid();
        await _gateway.SendCommandAsync<MarkMessageReadCommand, Unit>(
            new MarkMessageReadCommand(messageId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Care team replies to a patient on an existing thread.</summary>
    [HttpPost("provider/patients/{patientId:guid}/threads/{threadId:guid}/replies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> ReplyAsync(
        Guid patientId, Guid threadId, [FromBody] ReplyRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<ProviderReplyToMessageCommand, Guid>(
            new ProviderReplyToMessageCommand(patientId, threadId, body.ProviderId, body.Subject, body.Body),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/portal/messages/provider/patients/{patientId}/threads/{threadId}", new { id });
    }

    /// <summary>Care-team view of a patient's message threads (chart messaging card).</summary>
    [HttpGet("provider/patients/{patientId:guid}/threads")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageThreadView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPatientThreadsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var threads = await _gateway.SendQueryAsync<ListMessageThreadsForPatientQuery, IReadOnlyList<MessageThreadView>>(
            new ListMessageThreadsForPatientQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(threads);
    }

    /// <summary>Care-team view of the messages in one of a patient's threads.</summary>
    [HttpGet("provider/patients/{patientId:guid}/threads/{threadId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<SecureMessageView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientThreadAsync(Guid patientId, Guid threadId, CancellationToken cancellationToken)
    {
        var messages = await _gateway.SendQueryAsync<ListThreadMessagesQuery, IReadOnlyList<SecureMessageView>>(
            new ListThreadMessagesQuery(threadId), cancellationToken).ConfigureAwait(false);
        return Ok(messages.Where(m => m.PatientId == patientId).ToList());
    }

    /// <summary>Patient send-message request body.</summary>
    public sealed record SendMessageRequest(Guid? ThreadId, Guid? TargetProviderId, string Subject, string Body);

    /// <summary>Provider reply request body.</summary>
    public sealed record ReplyRequest(Guid ProviderId, string Subject, string Body);
}

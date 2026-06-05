using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.HIE.Inbound.Terminology;

/// <summary>Admin-visible row for an authored terminology resource (no FHIR body).</summary>
public sealed record AuthoredTerminologyRow(
    Guid Id, string ResourceType, string Url, string Version, string Status, string Name,
    DateTime UpdatedAtUtc, string UpdatedBy);

// -------- List --------

public sealed record ListAuthoredTerminologyQuery : IQuery<IReadOnlyList<AuthoredTerminologyRow>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TerminologyView;
}

public sealed class ListAuthoredTerminologyQueryHandler
    : IQueryHandler<ListAuthoredTerminologyQuery, IReadOnlyList<AuthoredTerminologyRow>>
{
    private readonly IAuthoredTerminologyRepository _repository;
    public ListAuthoredTerminologyQueryHandler(IAuthoredTerminologyRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AuthoredTerminologyRow>> HandleAsync(
        ListAuthoredTerminologyQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new AuthoredTerminologyRow(
            r.Id, r.ResourceType, r.Url, r.Version, r.Status, r.Name, r.UpdatedAtUtc, r.UpdatedBy))];
    }
}

// -------- Upsert (create or revise a (url, version) row) --------

public sealed record UpsertAuthoredTerminologyCommand(
    string ResourceType, string Url, string Version, string Status, string Name, string FhirJson, string UpdatedBy)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TerminologyAuthor;
}

public sealed class UpsertAuthoredTerminologyCommandHandler : ICommandHandler<UpsertAuthoredTerminologyCommand, Guid>
{
    private static readonly FhirJsonDeserializer _parser =
        new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    private readonly IAuthoredTerminologyRepository _repository;
    private readonly TimeProvider _clock;

    public UpsertAuthoredTerminologyCommandHandler(IAuthoredTerminologyRepository repository, TimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(UpsertAuthoredTerminologyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Validate the body parses, is the declared type, and carries the declared canonical url.
        // Authoring a malformed/mismatched resource fails closed before it can reach the catalog.
        ValidateBody(request.ResourceType, request.Url, request.FhirJson);

        var now = _clock.GetUtcNow().UtcDateTime;
        var existing = await _repository
            .FindByUrlVersionAsync(request.Url, request.Version, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            var created = new AuthoredTerminologyResource(
                Guid.CreateVersion7(), request.ResourceType, request.Url, request.Version,
                request.Status, request.Name, request.FhirJson, now, request.UpdatedBy);
            _repository.Add(created);
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return created.Id;
        }

        existing.Revise(request.Status, request.Name, request.FhirJson, now, request.UpdatedBy);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing.Id;
    }

    private static void ValidateBody(string resourceType, string url, string fhirJson)
    {
        Resource parsed;
        try
        {
            parsed = _parser.Deserialize<Resource>(fhirJson);
        }
        catch (Exception ex) when (ex is FormatException or DeserializationFailedException)
        {
            throw new ArgumentException("FhirJson is not a parseable FHIR resource: " + ex.Message, nameof(fhirJson));
        }

        if (!string.Equals(parsed.TypeName, resourceType, StringComparison.Ordinal))
            throw new ArgumentException(
                $"FhirJson is a {parsed.TypeName} but ResourceType is {resourceType}.", nameof(fhirJson));

        var bodyUrl = parsed switch
        {
            CodeSystem cs => cs.Url,
            ValueSet vs => vs.Url,
            ConceptMap cm => cm.Url,
            _ => null,
        };
        if (!string.Equals(bodyUrl, url, StringComparison.Ordinal))
            throw new ArgumentException(
                $"FhirJson canonical url '{bodyUrl}' does not match the declared Url '{url}'.", nameof(fhirJson));
    }
}

// -------- Set status (draft → active → retired) --------

public sealed record SetAuthoredTerminologyStatusCommand(Guid Id, string Status, string UpdatedBy)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TerminologyAuthor;
}

public sealed class SetAuthoredTerminologyStatusCommandHandler
    : ICommandHandler<SetAuthoredTerminologyStatusCommand, Unit>
{
    private readonly IAuthoredTerminologyRepository _repository;
    private readonly TimeProvider _clock;

    public SetAuthoredTerminologyStatusCommandHandler(IAuthoredTerminologyRepository repository, TimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Unit> HandleAsync(SetAuthoredTerminologyStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return Unit.Value;
        existing.SetStatus(request.Status, _clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// -------- Delete --------

public sealed record DeleteAuthoredTerminologyCommand(Guid Id) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TerminologyAuthor;
}

public sealed class DeleteAuthoredTerminologyCommandHandler : ICommandHandler<DeleteAuthoredTerminologyCommand, Unit>
{
    private readonly IAuthoredTerminologyRepository _repository;

    public DeleteAuthoredTerminologyCommandHandler(IAuthoredTerminologyRepository repository) => _repository = repository;

    public async Task<Unit> HandleAsync(DeleteAuthoredTerminologyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null) return Unit.Value;
        _repository.Remove(existing);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

using Dialysis.HIE.Xds.Domain;

namespace Dialysis.HIE.Xds.Ports;

public interface IXdsRegistry
{
    /// <summary>ITI-42: Register Document Set-b.</summary>
    ValueTask RegisterAsync(SubmissionSet submission, IReadOnlyList<DocumentEntry> documents, CancellationToken cancellationToken);

    /// <summary>ITI-18: Registry Stored Query.</summary>
    ValueTask<IReadOnlyList<DocumentEntry>> FindAsync(string patientId, CancellationToken cancellationToken);

    ValueTask<DocumentEntry?> GetByUniqueIdAsync(string uniqueId, CancellationToken cancellationToken);
}

public interface IXdsRepository
{
    /// <summary>ITI-41: Provide and Register Document Set-b — store the binary payload.</summary>
    ValueTask StoreAsync(string uniqueId, Stream content, CancellationToken cancellationToken);

    /// <summary>ITI-43: Retrieve Document Set.</summary>
    ValueTask<Stream> RetrieveAsync(string uniqueId, CancellationToken cancellationToken);
}

public interface IXdsDocumentStorage
{
    ValueTask<Stream> OpenWriteAsync(string uniqueId, CancellationToken cancellationToken);

    ValueTask<Stream> OpenReadAsync(string uniqueId, CancellationToken cancellationToken);
}

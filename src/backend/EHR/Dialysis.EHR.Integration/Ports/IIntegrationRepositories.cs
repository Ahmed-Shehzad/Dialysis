using Dialysis.EHR.Integration.Domain;

namespace Dialysis.EHR.Integration.Ports;

public interface IPharmacyTransmissionRepository
{
    Task<PharmacyTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PharmacyTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default);
    void Add(PharmacyTransmission transmission);
}

public interface ILabTransmissionRepository
{
    Task<LabTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LabTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default);
    void Add(LabTransmission transmission);
}

public interface IInsurerTransmissionRepository
{
    Task<InsurerTransmission?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InsurerTransmission?> FindByExternalControlNumberAsync(string controlNumber, CancellationToken cancellationToken = default);
    void Add(InsurerTransmission transmission);
}

/// <summary>Wire-protocol gateway for sending NCPDP SCRIPT prescriptions to a pharmacy.</summary>
public interface IPharmacyGateway
{
    Task<string> TransmitAsync(PharmacyTransmission transmission, CancellationToken cancellationToken);
}

public interface ILabGateway
{
    Task<string> TransmitAsync(LabTransmission transmission, CancellationToken cancellationToken);
}

public interface IInsurerGateway
{
    Task TransmitAsync(InsurerTransmission transmission, CancellationToken cancellationToken);
}

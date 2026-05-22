using Dialysis.SmartConnect.DataTypes.Ncpdp;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Ncpdp;

/// <summary>
/// Slice K2 port: maps a parsed NCPDP Telecom transaction onto a FHIR R4 resource.
/// Implementations are matched on <see cref="TransactionCode"/> by the
/// <c>NcpdpToFhirTransformStage</c>; returning <c>null</c> from <see cref="Map"/> tells
/// the dispatch stage to pass the payload through unchanged.
/// </summary>
public interface INcpdpToFhirMapper
{
    /// <summary>NCPDP transaction code (e.g. <c>B1</c>, <c>B2</c>, <c>E1</c>, <c>N1</c>).</summary>
    string TransactionCode { get; }

    Resource? Map(NcpdpTelecomMessage message);
}

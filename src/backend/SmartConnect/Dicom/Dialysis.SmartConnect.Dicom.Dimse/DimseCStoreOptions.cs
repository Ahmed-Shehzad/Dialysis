namespace Dialysis.SmartConnect.Dicom.Dimse;

/// <summary>
/// Options for the DIMSE C-STORE SCP hosted service. Port 11112 is the IANA-assigned DICOM port;
/// AET (Application Entity Title) is the 16-char identifier modalities use to address this SCP.
/// </summary>
public sealed class DimseCStoreOptions
{
    /// <summary>Local TCP port to listen on. Default 11112 (IANA DICOM).</summary>
    public int Port { get; set; } = 11112;

    /// <summary>Application Entity Title advertised to peers. Max 16 chars (DICOM PS3.7 §D.3.3.3).</summary>
    public string CalledAet { get; set; } = "SMARTCONNECT";

    /// <summary>
    /// Comma-separated AETs to accept. <c>"*"</c> accepts any. Production sites should list every
    /// expected modality AET; rejecting unknown peers reduces the attack surface of an open port.
    /// </summary>
    public string AllowedCallingAet { get; set; } = "*";
}

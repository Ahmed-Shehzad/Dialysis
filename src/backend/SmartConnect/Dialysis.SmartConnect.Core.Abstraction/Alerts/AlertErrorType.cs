namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Categories of runtime failure an <see cref="AlertRule"/> can match. Mirth UG pp 315-316
/// "Alert Error Types"; SmartConnect's stage taxonomy is slightly different but covers the same
/// observable error surface.
/// </summary>
public enum AlertErrorType
{
    Any = 0,
    OutboundFailure = 1,
    RouteFilterError = 2,
    TransformError = 3,
    PreProcessorError = 4,
    PostProcessorError = 5,
    AttachmentExtractError = 6,
}

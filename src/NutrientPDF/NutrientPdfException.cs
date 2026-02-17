namespace NutrientPDF;

/// <summary>
/// Exception thrown when a PDF operation fails. Includes the underlying GdPicture status when available.
/// </summary>
public sealed class NutrientPdfException : InvalidOperationException
{
    /// <summary>
    /// The GdPicture status code, if the failure originated from the GdPicture SDK.
    /// </summary>
    public string? GdPictureStatus { get; }

    public NutrientPdfException(string message)
        : base(message)
    {
    }

    public NutrientPdfException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NutrientPdfException(string message, string? gdPictureStatus)
        : base(message)
    {
        GdPictureStatus = gdPictureStatus;
    }

    public NutrientPdfException(string message, string? gdPictureStatus, Exception innerException)
        : base(message, innerException)
    {
        GdPictureStatus = gdPictureStatus;
    }

    internal static NutrientPdfException FromStatus(string operation, object status) =>
        new($"{operation} failed: {status}", status?.ToString());
}

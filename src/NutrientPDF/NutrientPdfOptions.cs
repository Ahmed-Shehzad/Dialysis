namespace NutrientPDF;

/// <summary>
/// Configuration options for NutrientPDF library.
/// </summary>
public sealed class NutrientPdfOptions
{
    /// <summary>
    /// License key for Nutrient .NET SDK. Leave empty for trial mode.
    /// Set once before any SDK operations. Obtain from https://www.nutrient.io/
    /// </summary>
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// Path to Chrome/Chromium browser executable. Required for HTML-to-PDF from URL.
    /// Example: @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
    /// </summary>
    public string? ChromePath { get; set; }
}

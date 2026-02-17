using GdPicture14;

using NutrientPDF.Abstractions;

namespace NutrientPDF.Adapter;

/// <summary>
/// Adapter that maps domain types to GdPicture SDK types (Adapter pattern).
/// Encapsulates all GdPicture-specific enumerations and conversions.
/// </summary>
internal static class GdPictureTypeAdapter
{
    public static PdfConversionConformance ToPdfConversionConformance(PdfAConformance c) => c switch
    {
        PdfAConformance.PdfA1a => PdfConversionConformance.PDF_A_1a,
        PdfAConformance.PdfA1b => PdfConversionConformance.PDF_A_1b,
        PdfAConformance.PdfA2a => PdfConversionConformance.PDF_A_2a,
        PdfAConformance.PdfA2u => PdfConversionConformance.PDF_A_2u,
        PdfAConformance.PdfA2b => PdfConversionConformance.PDF_A_2b,
        PdfAConformance.PdfA3a => PdfConversionConformance.PDF_A_3a,
        PdfAConformance.PdfA3u => PdfConversionConformance.PDF_A_3u,
        PdfAConformance.PdfA3b => PdfConversionConformance.PDF_A_3b,
        PdfAConformance.PdfA4 => PdfConversionConformance.PDF_A_4,
        PdfAConformance.PdfA4e => PdfConversionConformance.PDF_A_4e,
        PdfAConformance.PdfA4f => PdfConversionConformance.PDF_A_4f,
        _ => PdfConversionConformance.PDF_A_2a
    };

    public static PdfConformance ToPdfConformance(PdfAConformance c) => c switch
    {
        PdfAConformance.PdfA1a => PdfConformance.PDF_A_1a,
        PdfAConformance.PdfA1b => PdfConformance.PDF_A_1b,
        PdfAConformance.PdfA2a => PdfConformance.PDF_A_2a,
        PdfAConformance.PdfA2u => PdfConformance.PDF_A_2u,
        PdfAConformance.PdfA2b => PdfConformance.PDF_A_2b,
        PdfAConformance.PdfA3a => PdfConformance.PDF_A_3a,
        PdfAConformance.PdfA3u => PdfConformance.PDF_A_3u,
        PdfAConformance.PdfA3b => PdfConformance.PDF_A_3b,
        PdfAConformance.PdfA4 => PdfConformance.PDF_A_4,
        PdfAConformance.PdfA4e => PdfConformance.PDF_A_4e,
        PdfAConformance.PdfA4f => PdfConformance.PDF_A_4f,
        _ => PdfConformance.PDF_A_2a
    };

    public static PdfValidationConformance ToPdfValidationConformance(PdfAConformance c) => c switch
    {
        PdfAConformance.PdfA1a => PdfValidationConformance.PDF_A_1a,
        PdfAConformance.PdfA1b => PdfValidationConformance.PDF_A_1b,
        PdfAConformance.PdfA2a => PdfValidationConformance.PDF_A_2a,
        PdfAConformance.PdfA2u => PdfValidationConformance.PDF_A_2u,
        PdfAConformance.PdfA2b => PdfValidationConformance.PDF_A_2b,
        PdfAConformance.PdfA3a => PdfValidationConformance.PDF_A_3a,
        PdfAConformance.PdfA3u => PdfValidationConformance.PDF_A_3u,
        PdfAConformance.PdfA3b => PdfValidationConformance.PDF_A_3b,
        PdfAConformance.PdfA4 => PdfValidationConformance.PDF_A_4,
        PdfAConformance.PdfA4e => PdfValidationConformance.PDF_A_4e,
        PdfAConformance.PdfA4f => PdfValidationConformance.PDF_A_4f,
        _ => PdfValidationConformance.PDF_A_2a
    };

    public static PdfOcgState ToPdfOcgState(PdfLayerVisibility v) => v switch
    {
        PdfLayerVisibility.On => PdfOcgState.StateOn,
        PdfLayerVisibility.Off => PdfOcgState.StateOff,
        _ => PdfOcgState.Undefined
    };

    public static PdfLayerVisibility ToPdfLayerVisibility(PdfOcgState s) => s switch
    {
        PdfOcgState.StateOn => PdfLayerVisibility.On,
        PdfOcgState.StateOff => PdfLayerVisibility.Off,
        _ => PdfLayerVisibility.Undefined
    };

    public static GdPicture14.PdfPageLabelStyle ToPdfPageLabelStyle(Abstractions.PdfPageLabelStyle s) => s switch
    {
        Abstractions.PdfPageLabelStyle.DecimalArabicNumerals => GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleDecimalArabicNumerals,
        Abstractions.PdfPageLabelStyle.UppercaseRomanNumerals => GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleUppercaseRomanNumerals,
        Abstractions.PdfPageLabelStyle.LowercaseRomanNumerals => GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleLowercaseRomanNumerals,
        Abstractions.PdfPageLabelStyle.UppercaseLetters => GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleUppercaseLetters,
        Abstractions.PdfPageLabelStyle.LowercaseLetters => GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleLowercaseLetters,
        _ => GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleDecimalArabicNumerals
    };
}

using System.Diagnostics;

using GdPicture14;

namespace NutrientPDF.Helpers;

/// <summary>
/// Shared static utilities for PDF operations. Used by handlers.
/// </summary>
internal static class NutrientPdfHelpers
{
    internal const string OpLoadFromStream = "LoadFromStream";
    internal const string OpLoadFromFile = "LoadFromFile";
    internal const string OpSaveToStream = "SaveToStream";
    internal const string OpSaveToFile = "SaveToFile";

    internal static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("NutrientPDF: Failed to delete temp file {0}: {1}", path, ex.Message);
        }
    }

    internal static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("NutrientPDF: Failed to delete temp directory {0}: {1}", path, ex.Message);
        }
    }

    internal static Task RunAsync(Action action, CancellationToken ct) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            action();
        }, ct);

    internal static Task<T> RunAsync<T>(Func<T> func, CancellationToken ct) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return func();
        }, ct);

    internal static GdPicture14.DocumentFormat InferDocumentFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".docx" or ".doc" => GdPicture14.DocumentFormat.DocumentFormatDOCX,
            ".xlsx" or ".xls" => GdPicture14.DocumentFormat.DocumentFormatXLSX,
            ".pptx" or ".ppt" => GdPicture14.DocumentFormat.DocumentFormatPPTX,
            ".html" or ".htm" or ".mhtml" or ".mht" => GdPicture14.DocumentFormat.DocumentFormatHTML,
            ".txt" => GdPicture14.DocumentFormat.DocumentFormatTXT,
            ".rtf" => GdPicture14.DocumentFormat.DocumentFormatRTF,
            ".md" or ".markdown" => GdPicture14.DocumentFormat.DocumentFormatMD,
            ".msg" => GdPicture14.DocumentFormat.DocumentFormatMSG,
            ".eml" => GdPicture14.DocumentFormat.DocumentFormatEML,
            ".dxf" => GdPicture14.DocumentFormat.DocumentFormatDXF,
            ".odt" => GdPicture14.DocumentFormat.DocumentFormatODT,
            ".pdf" => GdPicture14.DocumentFormat.DocumentFormatPDF,
            _ => InferImageFormat(path)
        };
    }

    internal static GdPicture14.DocumentFormat InferImageFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => GdPicture14.DocumentFormat.DocumentFormatJPEG,
            ".png" => GdPicture14.DocumentFormat.DocumentFormatPNG,
            ".tiff" or ".tif" => GdPicture14.DocumentFormat.DocumentFormatTIFF,
            ".bmp" => GdPicture14.DocumentFormat.DocumentFormatBMP,
            ".svg" => GdPicture14.DocumentFormat.DocumentFormatSVG,
            ".gif" => GdPicture14.DocumentFormat.DocumentFormatGIF,
            ".webp" => GdPicture14.DocumentFormat.DocumentFormatWEBP,
            _ => GdPicture14.DocumentFormat.DocumentFormatUNKNOWN
        };
    }

    internal static void SaveImageToFile(GdPictureImaging imaging, int imageId, string outputPath)
    {
        var ext = Path.GetExtension(outputPath).ToLowerInvariant();
        switch (ext)
        {
            case ".jpg":
            case ".jpeg":
                imaging.SaveAsJPEG(imageId, outputPath);
                break;
            case ".tiff":
            case ".tif":
                imaging.SaveAsTIFF(imageId, outputPath, TiffCompression.TiffCompressionAUTO);
                break;
            default:
                imaging.SaveAsPNG(imageId, outputPath);
                break;
        }
    }
}

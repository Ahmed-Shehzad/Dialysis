namespace NutrientPDF.Abstractions;

/// <summary>
/// PDF form operations: fill, extract, export/import XFDF, add fields. Segregated interface (ISP).
/// </summary>
public interface IPdfFormsService
{
    Task<int> GetPdfFormFieldsCountAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task FillPdfFormFieldsAsync(string sourcePath, string outputPath, IReadOnlyDictionary<string, string> fieldValues, CancellationToken cancellationToken = default);
    Task FillPdfFormFieldsAsync(Stream sourceStream, Stream outputStream, IReadOnlyDictionary<string, string> fieldValues, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfFormFieldInfo>> ExtractPdfFormFieldsAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfFormFieldInfo>> ExtractPdfFormFieldsAsync(Stream sourceStream, CancellationToken cancellationToken = default);
    Task ExportPdfFormToXfdfAsync(string sourcePath, string xfdfOutputPath, bool exportAnnotations = false, CancellationToken cancellationToken = default);
    Task ExportPdfFormToXfdfAsync(Stream sourceStream, Stream xfdfOutputStream, bool exportAnnotations = false, CancellationToken cancellationToken = default);
    Task ImportPdfFormFromXfdfAsync(string sourcePath, string outputPath, string xfdfFilePath, bool importFormFields = true, bool importAnnotations = false, CancellationToken cancellationToken = default);
    Task ImportPdfFormFromXfdfAsync(Stream sourceStream, Stream outputStream, Stream xfdfStream, bool importFormFields = true, bool importAnnotations = false, CancellationToken cancellationToken = default);
    Task FlattenPdfFormFieldsAsync(string sourcePath, string outputPath, int? pageNumber = null, CancellationToken cancellationToken = default);
    Task FlattenPdfFormFieldsAsync(Stream sourceStream, Stream outputStream, int? pageNumber = null, CancellationToken cancellationToken = default);
    Task<int> AddPdfTextFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, string text = "", bool multiLine = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken cancellationToken = default);
    Task<int> AddPdfCheckBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, PdfCheckBoxStyle checkBoxStyle = PdfCheckBoxStyle.Check, bool @checked = false, byte checkMarkRed = 0, byte checkMarkGreen = 0, byte checkMarkBlue = 0, CancellationToken cancellationToken = default);
    Task<int> AddPdfComboBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, bool allowEdit = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken cancellationToken = default);
    Task<int> AddPdfListBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, bool sortItems = false, bool allowMultiple = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken cancellationToken = default);
    Task AddPdfFormFieldItemAsync(string sourcePath, string outputPath, int fieldId, string text, string? exportValue = null, CancellationToken cancellationToken = default);
    Task DeletePdfFormFieldItemAsync(string sourcePath, string outputPath, int fieldId, int itemIndex, CancellationToken cancellationToken = default);
    Task<int> GetPdfFormFieldItemCountAsync(string sourcePath, int fieldId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfFormFieldItem>> GetPdfFormFieldItemsAsync(string sourcePath, int fieldId, CancellationToken cancellationToken = default);
    Task RemovePdfFormFieldAsync(string sourcePath, string outputPath, int fieldId, CancellationToken cancellationToken = default);
    Task RemovePdfFormFieldsAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task SetPdfFormFieldValueAsync(string sourcePath, string outputPath, int fieldId, string value, CancellationToken cancellationToken = default);
    Task SetPdfFormFieldPropertiesAsync(string sourcePath, string outputPath, int fieldId, bool? readOnly = null, int? maxLength = null, PdfRgbColor? backgroundColor = null, CancellationToken cancellationToken = default);
}

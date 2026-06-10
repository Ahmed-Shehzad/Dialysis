using System.Reflection;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;

namespace Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;

/// <summary>
/// PDFsharp-backed AcroForms post-processor. Reads the input PDF, lazily creates an
/// <see cref="PdfAcroForm"/> if one is not present, and attaches a field per placement.
///
/// PDFsharp 6.x marks the AcroForm field constructors <c>internal</c> while the types
/// themselves are public. We instantiate fields through the documented internal-ctor path
/// (<c>Activator.CreateInstance(..., nonPublic: true)</c>) — a stable interop pattern that
/// the PDFsharp project documents as the workaround until a public field-builder API ships.
/// All field state is then set through the published property surface.
///
/// Threading: PDFsharp documents are not thread-safe. One <see cref="ApplyFormsAsync"/>
/// call owns one in-flight document; the processor itself is stateless and safe as a
/// singleton.
/// </summary>
public sealed class PdfSharpAcroFormProcessor : IAcroFormProcessor
{
    public async Task<byte[]> ApplyFormsAsync(
        ReadOnlyMemory<byte> pdfBytes,
        IReadOnlyList<AcroFormPlacement> placements,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(placements);
        if (placements.Count == 0)
        {
            return pdfBytes.ToArray();
        }

        EnsureUniqueNames(placements);
        // PDFsharp 6.x needs a font resolver wired up before any field construction; without
        // one the PdfTextField ctor throws on its default-appearance XFont creation.
        EmbeddedLatoFontResolver.EnsureRegistered();

        using var input = new MemoryStream(pdfBytes.ToArray(), writable: false);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        // Validate every placement's page reference before mutating the document — we don't
        // want a half-applied AcroForm dictionary if one placement is out of range.
        foreach (var placement in placements)
        {
            var pageIndex = placement.PageNumber - 1;
            if (pageIndex < 0 || pageIndex >= document.PageCount)
                throw new ArgumentOutOfRangeException(nameof(placements),
                    $"Placement references page {placement.PageNumber} but the PDF only has {document.PageCount} page(s).");
        }

        var acroForm = EnsureAcroForm(document);

        foreach (var placement in placements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageIndex = placement.PageNumber - 1;
            var page = document.Pages[pageIndex];
            var rect = ToPdfRectangle(placement.Origin, placement.Size);
            var field = CreateField(document, placement.Field, rect, page);
            AddFieldToForm(acroForm, field);
            AttachWidgetToPage(page, field, rect);
        }

        using var output = new MemoryStream();

        await document.SaveAsync(output).ConfigureAwait(false);

        return output.ToArray();
    }

    private static void EnsureUniqueNames(IReadOnlyList<AcroFormPlacement> placements)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in placements)
        {
            if (!seen.Add(p.Field.Name))
                throw new ArgumentException($"Duplicate AcroForm field name '{p.Field.Name}'.", nameof(placements));
        }
    }

    private static PdfAcroForm EnsureAcroForm(PdfDocument document)
    {
        // PDFsharp 6.x's `document.AcroForm` getter throws when no AcroForm dictionary is
        // attached, so we probe the catalog dictionary directly first.
        var catalog = document.Internals.Catalog.Elements;
        if (catalog.ContainsKey("/AcroForm"))
            return document.AcroForm;

        // Create + attach via the internal ctor; AcroForm has no public constructor.
        // The accessibility bypass is the documented PDFsharp workaround, not a security risk.
#pragma warning disable S3011
        var form = (PdfAcroForm?)Activator.CreateInstance(
            typeof(PdfAcroForm),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [document],
            culture: null)
            ?? throw new InvalidOperationException("PDFsharp could not construct a PdfAcroForm.");
#pragma warning restore S3011
        document.Internals.AddObject(form);
        catalog["/AcroForm"] = form.Reference;
        return form;
    }

    private static PdfAcroField CreateField(PdfDocument document, AcroFormField spec, PdfRectangle rect, PdfPage page)
    {
        var field = spec switch
        {
            TextFormField text => (PdfAcroField)BuildTextField(document, text),
            CheckBoxFormField checkBox => BuildCheckBoxField(document, checkBox),
            SignatureFormField => BuildSignatureField(document),
            ChoiceFormField choice => BuildChoiceField(document, choice),
            _ => throw new NotSupportedException($"AcroForm field type '{spec.GetType().Name}' is not yet supported."),
        };

        field.Elements.SetString("/T", spec.Name);
        // The field type (/FT) is what standards-compliant readers — pdfjs, Adobe, every viewer
        // that calls getFieldObjects — use to classify the field. PDFsharp's internal field ctors
        // (invoked here via reflection) don't serialize /FT, which leaves the widget's field type
        // null in the output PDF: the field renders but readers can't type it, so an interactive
        // form panel sees an empty field set. Set it explicitly per field kind.
        field.Elements.SetName("/FT", FieldTypeName(spec));
        if (!string.IsNullOrWhiteSpace(spec.Tooltip))
            field.Elements.SetString("/TU", spec.Tooltip);
        field.ReadOnly = spec.ReadOnly;
        if (spec.Required)
            field.Elements.SetInteger("/Ff", (int)PdfAcroFieldFlags.Required);

        // Widget annotation linkage so the field is visible on the page.
        field.Elements.SetName("/Subtype", "/Widget");
        field.Elements.SetRectangle("/Rect", rect);
        field.Elements.SetReference("/P", page);
        return field;
    }

    /// <summary>The PDF <c>/FT</c> field-type name for a placement spec.</summary>
    private static string FieldTypeName(AcroFormField spec) => spec switch
    {
        TextFormField => "/Tx",
        CheckBoxFormField => "/Btn",
        SignatureFormField => "/Sig",
        ChoiceFormField => "/Ch",
        _ => throw new NotSupportedException($"No /FT mapping for AcroForm field '{spec.GetType().Name}'."),
    };

    private static PdfTextField BuildTextField(PdfDocument document, TextFormField spec)
    {
        var field = NewField<PdfTextField>(document);
        if (!string.IsNullOrEmpty(spec.DefaultValue))
            field.Text = spec.DefaultValue;
        if (spec.MaxLength > 0)
            field.MaxLength = spec.MaxLength;
        field.MultiLine = spec.Multiline;
        field.Password = spec.Password;
        return field;
    }

    private static PdfCheckBoxField BuildCheckBoxField(PdfDocument document, CheckBoxFormField spec)
    {
        var field = NewField<PdfCheckBoxField>(document);
        // Set /V + /AS directly rather than via PdfCheckBoxField.Checked: the typed setter resolves
        // the "on" state from an /AP appearance dictionary, which a freshly-built field doesn't have
        // yet, so set_Checked(true) throws. The direct write is appearance-independent and valid.
        SetCheckBoxState(field, spec.DefaultChecked);
        return field;
    }

    /// <summary>
    /// Sets a checkbox's value + appearance state without going through PdfCheckBoxField.Checked
    /// (which throws when the field has no /AP appearance dictionary to read the on-state from).
    /// The on-state is the non-/Off key in /AP /N when present, else the conventional <c>/Yes</c>.
    /// </summary>
    private static void SetCheckBoxState(PdfAcroField field, bool on)
    {
        var onState = ResolveCheckBoxOnState(field) ?? "/Yes";
        var state = on ? onState : "/Off";
        field.Elements.SetName("/V", state);
        field.Elements.SetName("/AS", state);
    }

    private static string? ResolveCheckBoxOnState(PdfAcroField field)
    {
        var normal = field.Elements.GetDictionary("/AP")?.Elements.GetDictionary("/N");
        if (normal is not null)
        {
            foreach (var key in normal.Elements.Keys)
            {
                if (!string.Equals(key, "/Off", StringComparison.Ordinal))
                    return key;
            }
        }
        return null;
    }

    private static PdfSignatureField BuildSignatureField(PdfDocument document)
        => NewField<PdfSignatureField>(document);

    private static PdfChoiceField BuildChoiceField(PdfDocument document, ChoiceFormField spec)
    {
        var field = spec.AllowFreeText
            ? (PdfChoiceField)NewField<PdfComboBoxField>(document)
            : NewField<PdfListBoxField>(document);

        var optionsArray = new PdfArray(document);
        foreach (var option in spec.Options)
            optionsArray.Elements.Add(new PdfString(option));
        field.Elements["/Opt"] = optionsArray;
        if (!string.IsNullOrEmpty(spec.DefaultValue))
            field.Elements.SetString("/V", spec.DefaultValue);
        return field;
    }

    private static T NewField<T>(PdfDocument document) where T : PdfAcroField
    {
        // Same PDFsharp workaround as above: field ctors are internal by library design.
#pragma warning disable S3011
        var instance = (T?)Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [document],
            culture: null)
            ?? throw new InvalidOperationException($"PDFsharp could not construct a {typeof(T).Name}.");
#pragma warning restore S3011
        document.Internals.AddObject(instance);
        return instance;
    }

    private static void AddFieldToForm(PdfAcroForm form, PdfAcroField field)
    {
        var fieldsArray = form.Elements.GetArray("/Fields");
        if (fieldsArray is null)
        {
            fieldsArray = new PdfArray(form.Owner);
            form.Elements["/Fields"] = fieldsArray;
        }
        // Fields are indirect objects so the reference is always populated after AddObject.
        fieldsArray.Elements.Add(field.Reference!);
    }

    private static void AttachWidgetToPage(PdfPage page, PdfAcroField field, PdfRectangle rect)
    {
        var annotations = page.Elements.GetArray("/Annots");
        if (annotations is null)
        {
            annotations = new PdfArray(page.Owner);
            page.Elements["/Annots"] = annotations;
        }
        annotations.Elements.Add(field.Reference!);
        // Make sure the widget rectangle on the annotation matches the field rectangle.
        field.Elements.SetRectangle("/Rect", rect);
    }

    private static PdfRectangle ToPdfRectangle(PdfPoint origin, PdfSize size) =>
        new(new XRect(origin.X, origin.Y, size.Width, size.Height));

    public Task<AcroFormFillResult> FillFormValuesAsync(
        ReadOnlyMemory<byte> pdfBytes,
        IReadOnlyDictionary<string, string> fieldValues,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fieldValues);
        EmbeddedLatoFontResolver.EnsureRegistered();

        using var input = new MemoryStream(pdfBytes.ToArray(), writable: false);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        // Build a fully-qualified-name → field index over the existing AcroForm tree (handles
        // nested kid hierarchies — Acrobat / many partner forms wrap fields in group nodes).
        var index = new Dictionary<string, PdfAcroField>(StringComparer.Ordinal);
        var catalog = document.Internals.Catalog.Elements;
        if (catalog.ContainsKey("/AcroForm"))
        {
            IndexFields(document.AcroForm.Fields, parentName: null, index);
        }

        var filled = new List<string>(fieldValues.Count);
        var unknown = new List<string>();

        foreach (var (key, raw) in fieldValues)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!index.TryGetValue(key, out var field))
            {
                unknown.Add(key);
                continue;
            }
            // Signature fields are filled through IPdfSigner — silently skip them here so a
            // caller that round-trips the full field set doesn't accidentally overwrite the
            // signature dictionary. PDFsharp 6.x round-trips signature fields as either a
            // typed PdfSignatureField or a generic field with /FT=/Sig — check both.
            if (field is PdfSignatureField || IsSignatureField(field))
                continue;
            ApplyValue(field, raw);
            filled.Add(key);
        }

        using var output = new MemoryStream();
#pragma warning disable VSTHRD103
        document.Save(output);
#pragma warning restore VSTHRD103
        return Task.FromResult(new AcroFormFillResult(output.ToArray(), filled, unknown));
    }

    private static void IndexFields(PdfAcroField.PdfAcroFieldCollection fields, string? parentName, IDictionary<string, PdfAcroField> sink)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var localName = field.Elements.GetString("/T") ?? string.Empty;
            var qualified = string.IsNullOrEmpty(parentName) ? localName : $"{parentName}.{localName}";

            if (!string.IsNullOrEmpty(qualified))
                sink[qualified] = field;

            if (field.HasKids)
                IndexFields(field.Fields, qualified, sink);
        }
    }

    private static void ApplyValue(PdfAcroField field, string raw)
    {
        switch (field)
        {
            case PdfTextField text:
                text.Text = raw ?? string.Empty;
                break;
            case PdfCheckBoxField checkbox:
                SetCheckBoxState(checkbox, IsTruthy(raw));
                break;
            case PdfRadioButtonField radio:
                if (!string.IsNullOrEmpty(raw))
                    radio.Elements.SetName("/V", raw.StartsWith('/') ? raw : "/" + raw);
                break;
            case PdfChoiceField choice:
                if (!string.IsNullOrEmpty(raw))
                    choice.Elements.SetString("/V", raw);
                break;
            default:
                // Generic fallback: set the /V entry as a string. PDF readers will display it
                // even when PDFsharp doesn't model the field type directly.
                field.Elements.SetString("/V", raw ?? string.Empty);
                break;
        }
    }

    private static bool IsSignatureField(PdfAcroField field) =>
        string.Equals(field.Elements.GetName("/FT"), "/Sig", StringComparison.Ordinal);

    private static bool IsTruthy(string raw) =>
        raw is not null && (
            string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "on", StringComparison.OrdinalIgnoreCase) ||
            raw == "1" ||
            string.Equals(raw, "checked", StringComparison.OrdinalIgnoreCase));
}

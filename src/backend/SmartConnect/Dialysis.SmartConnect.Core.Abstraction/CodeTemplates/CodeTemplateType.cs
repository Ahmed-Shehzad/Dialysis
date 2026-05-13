namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// Mirth Code Template classification (UG p305). At runtime SmartConnect treats all three identically:
/// the body is prepended verbatim before the user script. The distinction is preserved for future editor UX.
/// </summary>
public enum CodeTemplateType
{
    /// <summary>Named JS function declaration; recommended for reusable helpers.</summary>
    Function = 0,

    /// <summary>Code snippet meant to be inserted by drag-and-drop in a UI editor.</summary>
    DragAndDropCodeBlock = 1,

    /// <summary>Code block executed once at the start of each script; no drag-and-drop equivalent.</summary>
    CompiledCodeBlock = 2,
}

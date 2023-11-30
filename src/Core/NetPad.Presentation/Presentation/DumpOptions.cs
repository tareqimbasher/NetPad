namespace NetPad.Presentation;

/// <summary>
/// Dump customization options.
/// </summary>
/// <param name="Title">If specified, will add a title heading to the result.</param>
/// <param name="CssClasses">If specified, will be added as CSS classes to the result.</param>
/// <param name="AppendNewLine">If true and the output is all text (not a nested structure), an additional line will be appended to the result.</param>
public record DumpOptions(
    string? Title = null,
    string? CssClasses = null,
    bool AppendNewLine = false,
    int? DestructAfterMs = null
)
{
    public static readonly DumpOptions Default = new();
}

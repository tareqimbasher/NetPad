namespace NetPad.Presentation;

/// <summary>
/// Dump customization options.
/// </summary>
/// <param name="Title">If specified, will add a title heading to the result.</param>
/// <param name="Id">If specified, will set the ID of the result. Useful when wanting to target a specific result.</param>
/// <param name="Tag">If specified, will tag the result. Useful when wanting to target multiple results as a group.</param>
/// <param name="CssClasses">If specified, will be added as CSS classes to the result.</param>
/// <param name="CodeType">If specified, assumes the dump target is a code string of this language and will
/// render with syntax highlighting.</param>
/// <param name="AppendNewLineToAllTextOutput">If true and the output is all text (not a nested structure), an additional line will be appended to the result.</param>
public record DumpOptions(
    string? Title = null,
    string? Id = null,
    string? Tag = null,
    string? CssClasses = null,
    string? CodeType = null,
    bool AppendNewLineToAllTextOutput = false,
    int? DestructAfterMs = null
)
{
    public static readonly DumpOptions Default = new();
}

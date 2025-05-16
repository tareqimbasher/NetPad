namespace NetPad.Presentation;

/// <summary>
/// Dump customization options.
/// </summary>
/// <param name="Title">
/// A heading displayed above the dumped output to help distinguish multiple dumps.
/// For example, <c>Dump(person, "Current User")</c> renders a “Current User” heading.
/// </param>
/// <param name="CssClasses">
/// One or more CSS class names to apply to the output container for styling the rendered dump.
/// You can use standard Bootstrap v5 class names (e.g., <c>"text-success"</c>, <c>"w-25"</c>), or specify custom classes
/// that you've defined under Settings &gt; Styles.
/// For example: <c>Dump(obj, css: "card text-bg-warning w-25")</c>
/// </param>
/// <param name="CodeType">
/// If you’re dumping a code snippet, specify its language (e.g. <c>"csharp"</c>, <c>"json"</c>, <c>"xml"</c>, etc.).
/// The output will be syntax-highlighted using <see href="https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md">Highlight.js</see>.
/// </param>
/// <param name="AppendNewLineToAllTextOutput">If true and the output is all text (not a nested structure), an additional line will be appended to the result.</param>
/// <param name="DestructAfterMs">
/// If provided, the dump will automatically be removed from the console after the given time in milliseconds.
/// For example, <c>clear: 5000</c> makes it disappear after 5 seconds.
/// </param>
public record DumpOptions(
    string? Title = null,
    string? CssClasses = null,
    string? CodeType = null,
    bool? AppendNewLineToAllTextOutput = null,
    int? DestructAfterMs = null
)
{
    /// <summary>
    /// Gets or sets the order of the dumped output. This is used to override the normal
    /// behaviour of the order being assigned automatically.
    /// </summary>
    internal uint? Order { get; init; }
}

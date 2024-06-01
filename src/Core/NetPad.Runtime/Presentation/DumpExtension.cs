namespace NetPad.Presentation;

public static class DumpExtension
{
    public static IDumpSink Sink { get; private set; }

    static DumpExtension()
    {
        Sink = new NullDumpSink();
    }

    public static void UseSink(IDumpSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        Sink = sink;
    }

    /// <summary>
    /// Dumps this object to the results console.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">If specified, will add a title heading to the result.</param>
    /// <param name="css">If specified, will add the specified CSS classes to the result.</param>
    /// <param name="code">If specified, assumes the dump target is a code string of this language and will
    /// render with syntax highlighting. See https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md for supported languages.</param>
    /// <param name="clear">If specified, will remove the result after specified milliseconds.</param>
    /// <returns>The same object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(this T? o, string? title = null, string? css = null, string? code = null, int? clear = null)
    {
        Sink.ResultWrite(o, new DumpOptions(
            Title: title,
            CssClasses: css,
            CodeType: code,
            DestructAfterMs: clear
        ));

        return o;
    }

    /// <summary>
    /// Dumps this <see cref="Span{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="Span{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <param name="cssClasses">If specified, will be added as CSS classes to the result.</param>
    /// <param name="clear">If specified, will remove the result after specified milliseconds.</param>
    /// <returns>The <see cref="Span{T}"/> being dumped.</returns>
    public static Span<T> Dump<T>(this Span<T> span, string? title = null, string? cssClasses = null, int? clear = null)
    {
        Sink.ResultWrite(span.ToArray(), new DumpOptions(
            Title: title,
            CssClasses: cssClasses
        ));
        return span;
    }

    /// <summary>
    /// Dumps this <see cref="ReadOnlySpan{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <param name="cssClasses">If specified, will be added as CSS classes to the result.</param>
    /// <param name="clear">If specified, will remove the result after specified milliseconds.</param>
    /// <returns>The <see cref="ReadOnlySpan{T}"/> being dumped.</returns>
    public static ReadOnlySpan<T> Dump<T>(this ReadOnlySpan<T> span, string? title = null, string? cssClasses = null, int? clear = null)
    {
        Sink.ResultWrite(span.ToArray(), new DumpOptions(
            Title: title,
            CssClasses: cssClasses
        ));
        return span;
    }
}

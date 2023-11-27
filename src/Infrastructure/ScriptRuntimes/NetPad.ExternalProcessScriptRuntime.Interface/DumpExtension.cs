namespace NetPad.Runtimes;

public static class DumpExtension
{
    /// <summary>
    /// Dumps this object to the results view.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(this T? o, string? title = null)
    {
        bool shouldAddNewLineAfter = false;

        if (ScriptRuntimeServices.OutputFormat == ExternalProcessOutputFormat.HTML)
        {
            // When using Dump() its implied that a new line is added to the end of it when rendered
            // When rendering objects or collections (ie. objects that are NOT rendered as strings)
            // they are rendered in an HTML block element that automatically pushes elements after it
            // to a new line. However when rendering strings (or objects that are rendered as strings)
            // HTML renders them in-line. So here we detect that, add manually add a new line
            shouldAddNewLineAfter = title == null && NetPad.Presentation.Html.HtmlPresenter.IsDotNetTypeWithStringRepresentation(typeof(T));
        }

        ScriptRuntimeServices.ResultWrite(o, title, appendNewLine: shouldAddNewLineAfter);

        return o;
    }

    /// <summary>
    /// Dumps this <see cref="Span{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="Span{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The <see cref="Span{T}"/> being dumped.</returns>
    public static Span<T> Dump<T>(this Span<T> span, string? title = null)
    {
        ScriptRuntimeServices.ResultWrite(span.ToArray(), title);
        return span;
    }

    /// <summary>
    /// Dumps this <see cref="ReadOnlySpan{T}"/> to the results view.
    /// </summary>
    /// <param name="span">The <see cref="ReadOnlySpan{T}"/> to dump.</param>
    /// <param name="title">An optional title for the result.</param>
    /// <returns>The <see cref="ReadOnlySpan{T}"/> being dumped.</returns>
    public static ReadOnlySpan<T> Dump<T>(this ReadOnlySpan<T> span, string? title = null)
    {
        ScriptRuntimeServices.ResultWrite(span.ToArray(), title);
        return span;
    }
}

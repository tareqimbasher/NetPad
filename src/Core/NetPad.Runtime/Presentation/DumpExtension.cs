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
    /// Dumps an object, or value, to the results console.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object being dumped. Can be a reference or value type.
    /// </typeparam>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">
    /// Optional. A heading displayed above the dumped output to help distinguish multiple dumps.
    /// For example, <c>Dump(person, "Current User")</c> renders a “Current User” heading.
    /// </param>
    /// <param name="css">
    /// Optional. One or more CSS class names to apply to the output container for styling the rendered dump.
    /// You can use standard Bootstrap v5 class names (e.g., <c>"text-success"</c>, <c>"w-25"</c>), or specify custom classes
    /// that you've defined under Settings &gt; Styles.
    /// For example: <c>Dump(obj, css: "card text-bg-warning w-25")</c>
    /// </param>
    /// <param name="code">
    /// Optional. If you’re dumping a code snippet, specify its language (e.g. <c>"csharp"</c>, <c>"json"</c>, <c>"xml"</c>, etc.).
    /// The output will be syntax-highlighted using <see href="https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md">Highlight.js</see>.
    /// </param>
    /// <param name="clear">
    /// Optional. If provided, the dump will automatically be removed from the console after the given time in milliseconds.
    /// For example, <c>clear: 5000</c> makes it disappear after 5 seconds.
    /// </param>
    /// <returns>
    /// Returns the same object instance (<paramref name="o"/>), allowing you to write:
    /// <code>
    /// var result = GetItems()
    ///     .Where(i => i.IsValid)
    ///     .Dump("Filtered Items")
    ///     .Select(i => i.Value);
    /// </code>
    /// </returns>
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
    /// Dumps an object, or value, to the results console, awaiting the call first.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object being dumped. Can be a reference or value type.
    /// </typeparam>
    /// <param name="o">The object to dump.</param>
    /// <param name="title">
    /// Optional. A heading displayed above the dumped output to help distinguish multiple dumps.
    /// For example, <c>Dump(person, "Current User")</c> renders a “Current User” heading.
    /// </param>
    /// <param name="css">
    /// Optional. One or more CSS class names to apply to the output container for styling the rendered dump.
    /// You can use standard Bootstrap v5 class names (e.g., <c>"text-success"</c>, <c>"w-25"</c>), or specify custom classes
    /// that you've defined under Settings &gt; Styles.
    /// For example: <c>Dump(obj, css: "card text-bg-warning w-25")</c>
    /// </param>
    /// <param name="code">
    /// Optional. If you’re dumping a code snippet, specify its language (e.g. <c>"csharp"</c>, <c>"json"</c>, <c>"xml"</c>, etc.).
    /// The output will be syntax-highlighted using <see href="https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md">Highlight.js</see>.
    /// </param>
    /// <param name="clear">
    /// Optional. If provided, the dump will automatically be removed from the console after the given time in milliseconds.
    /// For example, <c>clear: 5000</c> makes it disappear after 5 seconds.
    /// </param>
    /// <returns>
    /// Returns the same object instance (<paramref name="o"/>), allowing you to write:
    /// <code>
    /// var result = await GetItemsAsync().Dump("Filtered Items")
    /// </code>
    /// </returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static async Task<T?> Dump<T>(this Task<T?> o, string? title = null, string? css = null, string? code = null, int? clear = null)
    {
        var result = await o.ConfigureAwait(false);
        Sink.ResultWrite(result, new DumpOptions(
            Title: title,
            CssClasses: css,
            CodeType: code,
            DestructAfterMs: clear
        ));

        return result;
    }

    /// <summary>
    /// Dumps this object to the results console.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="options">Dump options.</param>
    /// <returns>The same object being dumped.</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    public static T? Dump<T>(this T? o, DumpOptions options)
    {
        Sink.ResultWrite(o, options);
        return o;
    }

    /// <summary>
    /// Dumps this object to the results console.
    /// </summary>
    /// <param name="o">The object to dump.</param>
    /// <param name="options">Dump options.</param>
    /// <returns>The same object being dumped.</returns>
    public static async Task<T> Dump<T>(this Task<T> o, DumpOptions options)
    {
        var result = await o.ConfigureAwait(false);
        Sink.ResultWrite(result, options);
        return result;
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

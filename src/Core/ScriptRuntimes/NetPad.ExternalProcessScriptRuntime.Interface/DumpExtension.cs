using NetPad.Presentation;
using NetPad.Presentation.Html;

namespace NetPad.Runtimes;

public static class DumpExtension
{
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
    public static T? Dump<T>(
        this T? o,
        string? title = null,
        string? css = null,
        string? code = null,
        int? clear = null,
        string? id = null)
    {
        bool shouldAddNewLineAfter = false;

        if (ScriptRuntimeServices.OutputFormat == ExternalProcessOutputFormat.HTML)
        {
            // When using Dump() its implied that a new line is added to the end of it when rendered
            // When rendering objects or collections (ie. objects that are NOT rendered as strings)
            // they are rendered in an HTML block element that automatically pushes elements after it
            // to a new line. However when rendering strings (or objects that are rendered as strings)
            // HTML renders them in-line. So here we detect that, add manually add a new line
            shouldAddNewLineAfter = title == null || HtmlPresenter.IsDotNetTypeWithStringRepresentation(typeof(T));
        }

        // if (id != null && id.All(char.IsWhiteSpace))
        // {
        //     id = null;
        // }

        ScriptRuntimeServices.ResultWrite(o, new DumpOptions(
            Id: id,
            Title: title,
            CssClasses: css,
            CodeType: code,
            DestructAfterMs: clear,
            AppendNewLineToAllTextOutput: shouldAddNewLineAfter
        ));

        return o;
    }

    // [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("o")]
    // public static T? DumpReplace<T>(
    //     this T? o,
    //     string id,
    //     string? title = null,
    //     string? css = null,
    //     string? code = null,
    //     int? clear = null)
    // {
    //     if (string.IsNullOrWhiteSpace(id))
    //     {
    //         throw new ArgumentNullException(nameof(id), "ID cannot be null or whitespace.");
    //     }
    //
    //     return o;
    // }


    public static LiveCollection<T> DumpLive<T>(
        this LiveCollection<T> collection,
        string? title = null,
        string? css = null,
        int? clear = null,
        Func<LiveCollection<T>, bool>? stopWhen = null,
        CancellationToken cancellationToken = default
    )
    {
        collection.StartLiveView(o =>
            {
                ScriptRuntimeServices.ResultWrite(
                    o,
                    new DumpOptions(
                        Id: collection.Id.ToString(),
                        Tag: $"live-collection:{collection.Id}",
                        Title: title,
                        CssClasses: css,
                        DestructAfterMs: clear
                    ));
            },
            stopWhen,
            cancellationToken
        );

        return collection;
    }

    public static PresentationView<T> DumpView<T>(
        this PresentationView<T> view,
        string? title = null,
        string? css = null,
        int? clear = null,
        Func<bool>? stopWhen = null,
        CancellationToken cancellationToken = default
    )
    {
        view.StartLiveView(o =>
            {
                ScriptRuntimeServices.ResultWrite(
                    o,
                    new DumpOptions(
                        Id: view.Id.ToString(),
                        Tag: $"live-collection:{view.Id}",
                        Title: title,
                        CssClasses: css,
                        DestructAfterMs: clear
                    ));
            },
            stopWhen,
            cancellationToken
        );

        return view;
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
        ScriptRuntimeServices.ResultWrite(span.ToArray(), new DumpOptions(
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
        ScriptRuntimeServices.ResultWrite(span.ToArray(), new DumpOptions(
            Title: title,
            CssClasses: cssClasses
        ));
        return span;
    }
}

namespace O2Html.Common;

internal static class Util
{
    /// <summary>
    /// Escapes special XHTML characters in a string.
    /// </summary>
    /// <param name="str">The string to escape.</param>
    /// <returns>The escaped string.</returns>
#if NETCOREAPP3_0_OR_GREATER
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("str")]
#endif
    public static string? EscapeStringForHtml(string? str)
    {
        if (str is null or "") return str;

        return str
            .ReplaceIfExists("&", HtmlConsts.HtmlAmpersand)
            .ReplaceIfExists(" ", HtmlConsts.HtmlSpace)
            .ReplaceIfExists("<", HtmlConsts.HtmlLessThan)
            .ReplaceIfExists(">", HtmlConsts.HtmlGreaterThan)
            .ReplaceIfExists("\"", HtmlConsts.HtmlQuote)
            .ReplaceIfExists("'", HtmlConsts.HtmlApostrophe)
            .ReplaceIfExists("\n", HtmlConsts.HtmlNewLine);
    }

#if NETCOREAPP3_0_OR_GREATER
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("str")]
#endif
    public static string? EscapeAngleBracketsForHtml(string? str)
    {
        if (str is null or "") return str;

        return str
            .ReplaceIfExists("<", HtmlConsts.HtmlLessThan)
            .ReplaceIfExists(">", HtmlConsts.HtmlGreaterThan);
    }
}

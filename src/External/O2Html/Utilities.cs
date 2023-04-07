using System;
using System.IO;
using System.Linq;
using System.Reflection;
using O2Html.Converters;

namespace O2Html;

public static class Utilities
{
    /// <summary>
    /// Escapes special XHTML characters in a string.
    /// </summary>
    /// <param name="str">The string to escape.</param>
    /// <returns>The escaped string.</returns>
#if NET6_0_OR_GREATER
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("str")]
#endif
    public static string? EscapeStringForHtml(string? str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        return str
            .ReplaceIfExists("&", Consts.HtmlAmpersand)
            .ReplaceIfExists(" ", Consts.HtmlSpace)
            .ReplaceIfExists("<", Consts.HtmlLessThan)
            .ReplaceIfExists(">", Consts.HtmlGreaterThan)
            .ReplaceIfExists("\"", Consts.HtmlQuote)
            .ReplaceIfExists("'", Consts.HtmlApostrophe)
            .ReplaceIfExists("\n", Consts.HtmlNewLine);
    }

    internal static string ReadEmbeddedResource(Assembly assembly, string name)
    {
        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        string? resourcePath = assembly.GetManifestResourceNames()
            .FirstOrDefault(str => str.EndsWith(name));

        if (resourcePath == null)
            throw new Exception($"Could not find embedded resource with name: {name}");

        using Stream? resourceStream = assembly.GetManifestResourceStream(resourcePath);
        if (resourceStream == null)
            throw new Exception($"Could not get embedded resource stream at path: {resourcePath}");

        using StreamReader reader = new(resourceStream);
        return reader.ReadToEnd();
    }
}

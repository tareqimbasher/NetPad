using System.IO;
using System.Text;

namespace NetPad.Utilities;

public static class StringUtil
{
    private static readonly string _bomString = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

    public static bool EqualsIgnoreCase(this string source, string value) => source.Equals(value, StringComparison.OrdinalIgnoreCase);
    public static bool ContainsIgnoreCase(this string source, string value) => source.Contains(value, StringComparison.OrdinalIgnoreCase);
    public static bool EndsWithIgnoreCase(this string source, string value) => source.EndsWith(value, StringComparison.OrdinalIgnoreCase);

    public static string JoinToString<T>(this IEnumerable<T> collection, string? separator) =>
        string.Join(separator, collection);

    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull("str")]
    public static string? Truncate(this string? str, int maxLength, bool withTrailingDots = false)
    {
        if (maxLength < 0 || str == null || str.Length <= maxLength)
        {
            return str;
        }

        return withTrailingDots ? $"{str[..maxLength]}..." : str[..maxLength];
    }

    public static string RemoveLeadingBOMString(string str) =>
        str.StartsWith(_bomString, StringComparison.Ordinal) ? str.Remove(0, _bomString.Length) : str;

    public static string DefaultIfNullOrWhitespace(this string? str, string defaultString = "") =>
        !string.IsNullOrWhiteSpace(str) ? str : defaultString;

    public static Uri? ToUriOrDefault(string uriString)
    {
        return Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    }

    public static string[] SplitLastOccurence(this string str, string separator)
    {
        int ix = str.LastIndexOf(separator, StringComparison.Ordinal);

        if (ix < 0)
        {
            return [str];
        }

        return
        [
            str[..ix],
            str[(ix + 1)..]
        ];
    }

    public static string SubstringBetween(this string str, string startDelimiter, string endDelimiter, bool useLastEndDelimiterOccurrence = false)
    {
        int from = str.IndexOf(startDelimiter, StringComparison.Ordinal) + startDelimiter.Length;
        int to = !useLastEndDelimiterOccurrence
            ? str.IndexOf(endDelimiter, StringComparison.Ordinal)
            : str.LastIndexOf(endDelimiter, StringComparison.Ordinal);

        var length = to - from;

        if (from < 0 || to < 0 || length < 0)
        {
            return str;
        }

        return str.Substring(from, to - from);
    }

    public static string RemoveInvalidFileNameCharacters(string str, string? replaceWith = null)
    {
        var invalid = Path.GetInvalidFileNameChars();

        return string.Join(replaceWith ?? string.Empty, str.Split(invalid, StringSplitOptions.None));
    }
}

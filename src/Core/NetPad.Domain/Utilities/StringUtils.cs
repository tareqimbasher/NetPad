using System;
using System.Collections.Generic;
using System.Text;

namespace NetPad.Utilities
{
    public static class StringUtils
    {
        private static readonly string _bomString = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

        public static string JoinToString<T>(this IEnumerable<T> collection, string? separator) =>
            string.Join(separator, collection);

        public static string RemoveLeadingBOMString(string str) =>
            str.StartsWith(_bomString, StringComparison.Ordinal) ? str.Remove(0, _bomString.Length) : str;

        public static string DefaultIfNullOrWhitespace(this string str, string defaultString = "") =>
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
                return new[] { str };
            }

            return new[]
            {
                str[..ix],
                str[(ix + 1)..]
            };
        }

        public static string SubstringBetween(this string str, string startDelimiter, string endDelimiter, bool useLastEndDelimiterOccurence = false)
        {
            int from = str.IndexOf(startDelimiter, StringComparison.Ordinal) + startDelimiter.Length;
            int to = !useLastEndDelimiterOccurence
                ? str.IndexOf(endDelimiter, StringComparison.Ordinal)
                : str.LastIndexOf(endDelimiter, StringComparison.Ordinal);

            if (from < 0 || to < 0)
            {
                return str;
            }

            return str.Substring(from, to - from);
        }
    }
}

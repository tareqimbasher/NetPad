using System;
using System.Collections.Generic;
using System.Text;

namespace NetPad.Utilities
{
    public static class StringUtils
    {
        private static readonly string _bomString = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

        public static string JoinToString<T>(this IEnumerable<T> collection, string? separator)
            => string.Join(separator, collection);

        public static string RemoveLeadingBOMString(string str) =>
            str.StartsWith(_bomString, StringComparison.Ordinal) ? str.Remove(0, _bomString.Length) : str;
    }
}

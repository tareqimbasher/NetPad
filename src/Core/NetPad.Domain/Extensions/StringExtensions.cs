using System.Collections.Generic;

namespace NetPad.Extensions
{
    public static class StringExtensions
    {
        public static string JoinToString<T>(this IEnumerable<T> collection, string? separator)
            => string.Join(separator, collection);
    }
}

using System;
using System.Text;

namespace NetPad.OmniSharpWrapper.Utilities
{
    public static class StringUtils
    {
        private static readonly string _bomString = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        public static string RemoveBOMString(string str) =>
            str.StartsWith(_bomString, StringComparison.Ordinal) ? str.Remove(0, _bomString.Length) : str;
    }
}
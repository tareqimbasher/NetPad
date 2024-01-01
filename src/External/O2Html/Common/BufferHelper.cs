using System.Collections.Generic;

namespace O2Html.Common;

internal static class BufferHelper
{
    public static List<byte> AddIndent(this List<byte> buffer, int indentLevel)
    {
        int spaces = indentLevel * 2;

        for (int i = 0; i < spaces; i++)
        {
            buffer.Add(HtmlConsts.SpaceByte);
        }

        return buffer;
    }
}

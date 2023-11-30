using System;
using System.Collections.Generic;

namespace O2Html.Common;

public static class HtmlConsts
{
    public const string HtmlAmpersand = "&amp;";
    public const string HtmlSpace = "&nbsp;";
    public const string HtmlLessThan = "&lt;";
    public const string HtmlGreaterThan = "&gt;";
    public const string HtmlQuote = "&quot;";
    public const string HtmlApostrophe = "&apos;";
    public const string HtmlNewLine = "<br/>";

    public const byte OpeningAngleBracketByte = 0x3C;
    public const byte ClosingAngleBracketByte = 0x3E;
    public const byte ForwardSlashByte = 0x2F;
    public const byte SpaceByte = 0x20;
    public const byte NewLineByte = 0x0A;

    public static readonly HashSet<string> SelfClosingTags = new(new[]
    {
        "area",
        "base",
        "br",
        "col",
        "embed",
        "hr",
        "img",
        "input",
        "link",
        "meta",
        "param",
        "source",
        "track",
        "wbr",
    }, StringComparer.OrdinalIgnoreCase);
}

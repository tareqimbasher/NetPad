using System.Collections.Generic;
using System.Text;
using O2Html.Common;

namespace O2Html.Dom;

/// <summary>
/// A full HTML document.
/// </summary>
public class HtmlDocument : Element
{
    public const string HtmlDocType = "<!DOCTYPE html>";

    public HtmlDocument() : base("html")
    {
        Head = this.AddAndGetElement("head");
        Head.AddAndGetElement("meta").SetAttribute("charset","UTF-8");
        Body = this.AddAndGetElement("body");
    }

    public Element Head { get; }

    public Element Body { get; }

    public HtmlDocument AddStyle(string code)
    {
        Head.AddAndGetElement("style").AddText(code);
        return this;
    }

    public TextNode AddAndGetStyle(string code)
    {
        return Head.AddAndGetElement("style").AddAndGetText(code);
    }

    public override void ToHtml(List<byte> buffer, Formatting? formatting = null, int indentLevel = 0)
    {
        buffer.AddRange(Encoding.UTF8.GetBytes(HtmlDocType));

        if (formatting == Formatting.Indented)
        {
            buffer.Add(HtmlConsts.NewLineByte);
        }

        base.ToHtml(buffer, formatting, indentLevel);
    }
}

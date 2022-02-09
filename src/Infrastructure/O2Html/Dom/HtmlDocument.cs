using System.Collections.Generic;
using O2Html.Scripts;
using O2Html.Styles;

namespace O2Html.Dom;

public class HtmlDocument : Element
{
    public HtmlDocument() : base("html")
    {
        Head = this.AddAndGetElement("head");
        Head.AddElement("<meta charset=\"UTF-8\">");
        Body = this.AddAndGetElement("body");

        Styles = new List<IStyleSheet>();
        Scripts = new List<IScript>();
    }

    public Element Head { get; }

    public Element Body { get; }

    public List<IStyleSheet> Styles { get; }

    public List<IScript> Scripts { get; }

    public HtmlDocument AddStyle(IStyleSheet style)
    {
        Styles.Add(style);
        return this;
    }

    public HtmlDocument AddScript(IScript script)
    {
        Scripts.Add(script);
        return this;
    }

    public HtmlDocument Append<T>(T? obj)
    {
        var element = HtmlConvert.Serialize(obj);
        Body.AddChild(element);
        return this;
    }

    public override string ToHtml(Formatting? formatting = null)
    {
        foreach (var style in Styles)
            Head.AddAndGetElement("style").AddText(style.GetCode());

        foreach (var script in Scripts)
            Body.AddAndGetElement("script").SetOrAddAttribute("type", "text/javascript").Element
                .AddText(script.GetCode());

        return $"<!DOCTYPE html> {base.ToHtml(formatting)}";
    }
}

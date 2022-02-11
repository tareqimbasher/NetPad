using System.Reflection;
using O2Html.EmbeddedResources;

namespace O2Html.Styles;

public class EmbeddedStyleSheet : EmbeddedResource, IStyleSheet
{
    public EmbeddedStyleSheet(string name) : base(name)
    {
    }

    public EmbeddedStyleSheet(string name, Assembly assembly) : base(name, assembly)
    {
    }

    public string GetCode() => GetContents();
}

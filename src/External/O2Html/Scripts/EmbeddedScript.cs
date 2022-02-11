using System.Reflection;
using O2Html.EmbeddedResources;

namespace O2Html.Scripts;

public class EmbeddedScript : EmbeddedResource, IScript
{
    public EmbeddedScript(string name) : base(name)
    {
    }

    public EmbeddedScript(string name, Assembly assembly) : base(name, assembly)
    {
    }

    public string GetCode() => GetContents();
}
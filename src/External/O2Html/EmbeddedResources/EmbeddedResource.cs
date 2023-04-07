using System.Reflection;

namespace O2Html.EmbeddedResources;

public class EmbeddedResource
{
    public EmbeddedResource(string name) : this(name, Assembly.GetExecutingAssembly())
    {
        Name = name;
    }

    public EmbeddedResource(string name, Assembly assembly)
    {
        Name = name;
        Assembly = assembly;
    }

    public string Name { get; }
    public Assembly Assembly { get; }

    public string GetContents() => Utilities.ReadEmbeddedResource(Assembly, Name);
}

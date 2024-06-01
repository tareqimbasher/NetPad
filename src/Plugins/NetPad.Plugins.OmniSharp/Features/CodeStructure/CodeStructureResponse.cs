namespace NetPad.Plugins.OmniSharp.Features.CodeStructure;

/// <summary>
/// Used to be able to deserialize CodeElement type from OmniSharp Server return
/// The original CodeElement from OmniSharp.Models fails to deserialize using STJ
/// </summary>
public class CodeStructureResponse
{
    public IReadOnlyList<CodeElement>? Elements { get; set; }

    public class CodeElement(
        string kind,
        string name,
        string displayName,
        IReadOnlyList<CodeElement> children,
        Dictionary<string, OmniSharpRange> ranges,
        IReadOnlyDictionary<string, object> properties)
    {
        public string Kind { get; } = kind;
        public string Name { get; } = name;
        public string DisplayName { get; } = displayName;
        public IReadOnlyList<CodeElement> Children { get; } = children;
        public Dictionary<string, OmniSharpRange> Ranges { get; } = ranges;
        public IReadOnlyDictionary<string, object> Properties { get; } = properties;

        public override string ToString() => $"{Kind} {Name}";
    }
}

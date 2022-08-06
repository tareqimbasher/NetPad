namespace NetPad.Plugins.OmniSharp.Features.CodeStructure;

/// <summary>
/// Used to be able to deserialize CodeElement type from OmniSharp Server return
/// The original CodeElement from OmniSharp.Models fails to deserialize using STJ
/// </summary>
public class CodeStructureResponse
{
    public IReadOnlyList<CodeElement>? Elements { get; set; }

    public class CodeElement
    {
        public CodeElement(
            string kind,
            string name,
            string displayName,
            IReadOnlyList<CodeElement> children,
            Dictionary<string, OmniSharpRange> ranges,
            IReadOnlyDictionary<string, object> properties)
        {
            Kind = kind;
            Name = name;
            DisplayName = displayName;
            Children = children;
            Ranges = ranges;
            Properties = properties;
        }

        public string Kind { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public IReadOnlyList<CodeElement> Children { get; }
        public Dictionary<string, OmniSharpRange> Ranges { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }

        public override string ToString()
            => $"{Kind} {Name}";
    }
}

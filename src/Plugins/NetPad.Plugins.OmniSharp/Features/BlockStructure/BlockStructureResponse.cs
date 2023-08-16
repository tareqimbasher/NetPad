namespace NetPad.Plugins.OmniSharp.Features.BlockStructure;

public class BlockStructureResponse
{
    public IEnumerable<CodeFoldingBlock> Spans { get; set; } = null!;

    public class CodeFoldingBlock
    {
        /// <summary>
        /// The span of text to collapse.
        /// </summary>
        public OmniSharpRange Range { get; init; } = null!;

        /// <summary>
        /// If the block is one of the types specified in <see cref="OmniSharpModels.V2.CodeFoldingBlockKinds"/>, that type.
        /// Otherwise, null.
        /// </summary>
        public string? Kind { get; init; }
    }
}

namespace NetPad.Plugins.OmniSharp.Features.InlayHinting;

/// <summary>
/// Used to bind <see cref="InlayHint"/> when posting it to API endpoint. STJ does not know
/// how to deserialize {item1: 0, item2: 1} into a ValueTuple(long, int)
/// </summary>
public record InlayHintResolveRequest : OmniSharpInlayHintResolveRequest
{
    /// <summary>
    /// Hides base Hint property
    /// </summary>
    public new InlayHint Hint { get; set; } = null!;

    public OmniSharpInlayHintResolveRequest ToOmniSharpInlayHintResolveRequest()
    {
        return new OmniSharpInlayHintResolveRequest
        {
            Hint = Hint.ToOmniSharpInlayHint()
        };
    }

    public class InlayHint // Cannot inherit from OmniSharp InlayHint, it is sealed
    {
        public OmniSharpPoint Position { get; set; } = null!;
        public string Label { get; set; } = null!;
        public string? Tooltip { get; set; }
        public InlayHintData Data { get; set; } = null!;

        public OmniSharpInlayHint ToOmniSharpInlayHint()
        {
            return new OmniSharpInlayHint
            {
                Label = Label,
                Tooltip = Tooltip,
                Position = Position,
                Data = (Data.Item1, Data.Item2)
            };
        }
    }

    public class InlayHintData
    {
        public string Item1 { get; set; } = null!;
        public int Item2 { get; set; }
    }
}

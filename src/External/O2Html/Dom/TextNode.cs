namespace O2Html.Dom;

public class TextNode : Node
{
    public TextNode(string text) : base(NodeType.Text)
    {
        Text = text;
    }

    public string Text { get; private set; }

    public void SetText(string text)
    {
        Text = text;
    }

    public override string ToHtml(Formatting? formatting = null)
    {
        return Text ?? string.Empty;
    }

    public override string ToString() => ToHtml();
}

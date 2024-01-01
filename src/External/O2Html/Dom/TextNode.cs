using System.Collections.Generic;
using System.Text;
using O2Html.Common;

namespace O2Html.Dom;

/// <summary>
/// A HTML Text-type Node.
/// </summary>
public class TextNode : Node
{
    /// <summary>
    /// Instantiates a new <see cref="TextNode"/>.
    /// </summary>
    /// <param name="text">The text contents.</param>
    /// <param name="isEscaped">Whether the HTML representation of the text within this node should be escaped.</param>
    public TextNode(string? text, bool isEscaped = false) : base(NodeType.Text)
    {
        Text = text;
        IsEscaped = isEscaped;
    }

    public string? Text { get; private set; }

    /// <summary>
    /// Whether the HTML representation of the text within this node should be escaped.
    /// </summary>
    public bool IsEscaped { get; set; }

    /// <summary>
    /// Updates the text value of this node.
    /// </summary>
    /// <param name="text">The text contents.</param>
    /// <param name="isEscaped">Whether the HTML representation of the text within this node should be escaped.</param>
    public void SetText(string? text, bool? isEscaped = null)
    {
        Text = text;
        if (isEscaped != null)
            IsEscaped = isEscaped.Value;
    }

    public override string ToHtml(Formatting? formatting = null, int indentLevel = 0)
    {
        var buffer = new List<byte>();
        ToHtml(buffer, formatting);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public override void ToHtml(List<byte> buffer, Formatting? formatting = null, int indentLevel = 0)
    {
        if (string.IsNullOrEmpty(Text)) return;

        if (formatting == Formatting.Indented)
        {
            buffer.AddIndent(indentLevel);
        }

        var text = IsEscaped ? Util.EscapeStringForHtml(Text)! : Text!;
        buffer.AddRange(Encoding.UTF8.GetBytes(text));
    }

    public override string ToString() => ToHtml();

    public static TextNode RawText(string rawText)
    {
        return new TextNode(rawText);
    }

    public static TextNode EscapedText(string text)
    {
        return new TextNode(text, isEscaped: true);
    }
}

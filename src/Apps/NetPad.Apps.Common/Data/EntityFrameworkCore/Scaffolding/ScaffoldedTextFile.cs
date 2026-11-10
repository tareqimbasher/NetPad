namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedTextFile(string path, string text)
{
    public string Path { get; } = path;
    public string Text { get; set; } = text;
}

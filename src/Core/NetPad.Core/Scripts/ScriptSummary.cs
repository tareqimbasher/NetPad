namespace NetPad.Scripts
{
    public class ScriptSummary
    {
        public ScriptSummary(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; set; }
        public string Path { get; set; }
    }
}

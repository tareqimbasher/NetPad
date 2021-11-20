namespace NetPad.Queries
{
    public class QuerySummary
    {
        public QuerySummary(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; set; }
        public string Path { get; set; }
    }
}

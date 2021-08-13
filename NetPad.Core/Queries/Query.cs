using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetPad.Exceptions;

namespace NetPad.Queries
{
    public class Query
    {
        public Query(string filePath)
        {
            FilePath = filePath;
            Config = new QueryConfig(); 
            // Code = string.Empty;
            Code = "Console.WriteLine(\"Hello World\");";
        }

        public string FilePath { get; set; }
        public string Name => Path.GetFileNameWithoutExtension(FilePath);
        public string DirectoryPath => Path.GetDirectoryName(FilePath)!;
        
        public QueryConfig Config { get; private set; }
        public string Code { get; set; }
        public bool IsDirty { get; private set; }


        public async Task LoadAsync()
        {
            var text = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);

            var parts = text.Split("#Query");
            if (parts.Length != 2)
                throw new InvalidQueryFormat(FilePath);

            Config = JsonSerializer.Deserialize<QueryConfig>(parts[0]) ?? throw new InvalidQueryFormat(FilePath);
            Code = parts[1].TrimStart();
        }

        public async Task SaveAsync()
        {
            var config = JsonSerializer.Serialize(Config);

            await File.WriteAllTextAsync(FilePath, $"{config}\n" +
                                                   $"#Query\n" +
                                                   $"{Code}")
                .ConfigureAwait(false);
            IsDirty = false;
        }

        public async Task UpdateCodeAsync(string newCode)
        {
            Code = newCode ?? string.Empty;
            IsDirty = true;
        }

        public async Task GetRunnableCodeAsync()
        {
            var defaultNs = new[]
            {
                "System",
                "System.IO",
                "System.Collections",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks",
            };

            var code = new StringBuilder();

            foreach (var ns in defaultNs.Union(Config.Namespaces).Distinct())
            {
                code.AppendLine($"using {ns};");
            }

            // Namespace
            code.AppendLine("namespace QueryRuntime");
            code.AppendLine("{");

            // Class
            code.AppendLine("public class Program");
            code.AppendLine("{");
            
            // Properties
            code.AppendLine("public Exception? Exception { get; }");
            
            // Main method
            code.AppendLine("public async Task Main()");
            code.AppendLine("{");
            
            
            code.AppendLine("try");
            code.AppendLine("{");
            
            code.AppendLine(Code);

            code.AppendLine("}");
            code.AppendLine("catch (Exception ex)");
            code.AppendLine("{");
            code.AppendLine("this.Exception = ex;");
            code.AppendLine("}");
            
            
            code.AppendLine("}"); // Main method
            code.AppendLine("}"); // Class
            code.AppendLine("}"); // Namespace
        }
    }
}
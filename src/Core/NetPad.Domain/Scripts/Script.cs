using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Exceptions;

namespace NetPad.Scripts
{
    public class Script : INotifyOnPropertyChanged
    {
        private bool _isDirty = false;
        private string _name;

        public Script(Guid id, string name)
        {
            Id = id;
            _name = name;
            Config = new ScriptConfig(ScriptKind.Statements, new List<string>());
            Code = string.Empty;
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();
        }

        public Script(string name) : this(Guid.NewGuid(), name)
        {
        }

        [JsonIgnore]
        public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }

        public Guid Id { get; private set; }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string? Path { get; private set; }
        public ScriptConfig Config { get; private set; }
        public string Code { get; private set; }

        public string? DirectoryPath => Path == null ? null : System.IO.Path.GetDirectoryName(Path);

        public bool IsDirty
        {
            get => _isDirty || IsNew;
            set => this.RaiseAndSetIfChanged(ref _isDirty, value);
        }

        public bool IsNew => Path == null;


        public async Task LoadAsync(string contents)
        {
            if (Path == null)
                throw new InvalidOperationException($"Path: '{Path}' is null. Cannot load script.");

            var parts = contents.Split("#Code");
            if (parts.Length != 2)
                throw new InvalidScriptFormat(Path);

            var part1 = parts[0];
            var part1Lines = part1.Split(Environment.NewLine);
            var part2 = parts[1];

            Id = Guid.Parse(part1Lines.First());
            Name = System.IO.Path.GetFileNameWithoutExtension(Path);
            Config = JsonSerializer.Deserialize<ScriptConfig>(
                string.Join(Environment.NewLine, part1Lines.Skip(1))) ?? throw new InvalidScriptFormat(Path);
            Code = part2.TrimStart();
        }

        public void SetPath(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));

            if (!path.StartsWith("/")) path = "/" + path;

            if (!path.EndsWith(".netpad")) path += ".netpad";

            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public void UpdateCode(string newCode)
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
            code.AppendLine("namespace ScriptRuntime");
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

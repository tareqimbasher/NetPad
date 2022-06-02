using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Exceptions;

namespace NetPad.Scripts
{
    public class Script : INotifyOnPropertyChanged
    {
        public const string STANDARD_EXTENSION_WO_DOT = "netpad";
        public const string STANDARD_EXTENSION = ".netpad";
        private string _name;
        private string _code;
        private string? _path;
        private bool _isDirty;

        public Script(Guid id, string name)
        {
            if (id == default)
                throw new ArgumentException($"{nameof(id)} cannot be an empty GUID");

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} cannot be an empty or whitespace");

            Id = id;
            _name = name;
            _code = string.Empty;
            Config = new ScriptConfig(ScriptKind.Statements);
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();
        }

        public Script(string name) : this(Guid.NewGuid(), name)
        {
        }

        [JsonIgnore] public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }

        public Guid Id { get; private set; }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string? Path
        {
            get => _path;
            private set => this.RaiseAndSetIfChanged(ref _path, value);
        }

        public ScriptConfig Config { get; private set; }

        public string Code
        {
            get => _code;
            private set => this.RaiseAndSetIfChanged(ref _code, value);
        }

        public bool IsDirty
        {
            get => _isDirty;
            set => this.RaiseAndSetIfChanged(ref _isDirty, value);
        }

        public string? DirectoryPath => Path == null ? null : System.IO.Path.GetDirectoryName(Path);

        public bool IsNew => Path == null;


        public string Serialize()
        {
            return $"{Id}\n" +
                   $"{JsonSerializer.Serialize(Config)}\n" +
                   $"#Code\n" +
                   $"{Code}";
        }

        public void Deserialize(string contents)
        {
            var parts = contents.Split("#Code");
            if (parts.Length != 2)
                throw new InvalidScriptFormatException(this, "The script is missing #Code identifier.");

            var part1 = parts[0];
            var part1Lines = part1.Split("\n");
            var part2 = parts[1];

            if (!Guid.TryParse(part1Lines.First(), out var id) || id == default)
                throw new InvalidScriptFormatException(this, "Invalid or non-existent ID.");
            else
                Id = id;

            Code = part2.TrimStart();

            Config.RemoveAllPropertyChangedHandlers();

            Config = JsonSerializer.Deserialize<ScriptConfig>(
                string.Join(Environment.NewLine, part1Lines.Skip(1))) ?? throw new InvalidScriptFormatException(this, "Invalid config section.");

            Config.OnPropertyChanged.Add(change  =>
            {
                IsDirty = true;
                return Task.CompletedTask;
            });
        }

        public void SetPath(string path)
        {
            if (Path == path)
                return;

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (!path.EndsWith(STANDARD_EXTENSION)) path += STANDARD_EXTENSION;

            Path = path.Replace('\\', '/');
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public void UpdateCode(string? newCode)
        {
            if (Code == newCode)
                return;

            Code = newCode ?? string.Empty;
            IsDirty = true;
        }

        public override string ToString()
        {
            return $"[{Id}] {Name}".TrimEnd();
        }

        public static Script From(string name, string scriptJson, string? path)
        {
            var newScript = new Script(name);
            newScript.Deserialize(scriptJson);

            if (path != null)
                newScript.SetPath(path);

            return newScript;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Exceptions;

namespace NetPad.Scripts
{
    public class Script : INotifyOnPropertyChanged
    {
        public const string STANARD_EXTENSION_WO_DOT = "netpad";
        public const string STANARD_EXTENSION = ".netpad";
        private bool _isDirty = false;
        private string _name;

        public Script(Guid id, string name)
        {
            Id = id;
            _name = name;
            Config = new ScriptConfig(ScriptKind.Statements, ScriptConfigDefaults.DefaultNamespaces);
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

        public bool IsDirty
        {
            get => _isDirty || IsNew;
            set => this.RaiseAndSetIfChanged(ref _isDirty, value);
        }

        public string? DirectoryPath => Path == null ? null : System.IO.Path.GetDirectoryName(Path);

        public bool IsNew => Path == null;


        public Task LoadAsync(string contents)
        {
            var parts = contents.Split("#Code");
            if (parts.Length != 2)
                throw new InvalidScriptFormat(this);

            var part1 = parts[0];
            var part1Lines = part1.Split(Environment.NewLine);
            var part2 = parts[1];

            Id = Guid.Parse(part1Lines.First());
            Config = JsonSerializer.Deserialize<ScriptConfig>(
                string.Join(Environment.NewLine, part1Lines.Skip(1))) ?? throw new InvalidScriptFormat(this);
            Code = part2.TrimStart();

            return Task.CompletedTask;
        }

        public void SetPath(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));

            if (!path.StartsWith("/")) path = "/" + path;

            if (!path.EndsWith(STANARD_EXTENSION)) path += STANARD_EXTENSION;

            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public void UpdateCode(string newCode)
        {
            Code = newCode ?? string.Empty;
            IsDirty = true;
        }
    }
}

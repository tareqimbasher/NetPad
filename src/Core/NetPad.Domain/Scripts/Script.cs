using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Data;

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

        public Script(Guid id, string name, ScriptConfig config, string code)
        {
            if (id == default)
                throw new ArgumentException($"{nameof(id)} cannot be an empty GUID");

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} cannot be an empty or whitespace");

            Id = id;
            _name = name;
            _code = code;
            Config = config;
            Config.OnPropertyChanged.Add(ConfigPropertyChangedHandler);
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();
        }

        public Script(Guid id, string name, ScriptConfig config) : this(id, name, config, string.Empty)
        {
        }

        public Script(Guid id, string name) : this(id, name, new ScriptConfig(ScriptKind.Program))
        {
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


        public ScriptConfig Config { get; }

        public DataConnection? DataConnection { get; private set; }

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

        public void SetPath(string path)
        {
            if (Path == path)
                return;

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (!path.EndsWith(STANDARD_EXTENSION)) path += STANDARD_EXTENSION;

            Path = path.Replace('\\', '/');
            Name = GetNameFromPath(path);
        }

        public void UpdateCode(string? newCode)
        {
            if (Code == newCode)
                return;

            Code = newCode ?? string.Empty;
            IsDirty = true;
        }

        public void SetDataConnection(DataConnection? dataConnection)
        {
            DataConnection = dataConnection;
        }

        public override string ToString()
        {
            return $"[{Id}] {Name}".TrimEnd();
        }

        private Task ConfigPropertyChangedHandler(PropertyChangedArgs propertyChangedArgs)
        {
            IsDirty = true;
            return Task.CompletedTask;
        }

        public static string GetNameFromPath(string path) => System.IO.Path.GetFileNameWithoutExtension(path);
    }
}

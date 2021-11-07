using System;
using System.Diagnostics;
using System.IO;

namespace OmniSharp.Stdio
{
    public class OmniSharpStdioServerConfigurationBuilder
    {
        private Func<Process>? _processGetter;
        private string? _executablePath;
        private string? _executableArgs;

        public OmniSharpStdioServerConfigurationBuilder UseExistingInstance(Func<Process> processGetter)
        {
            _processGetter = processGetter ?? throw new ArgumentNullException(nameof(processGetter));
            return this;
        }

        public OmniSharpStdioServerConfigurationBuilder UseNewInstance(string executablePath, string args)
        {
            _executablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            _executableArgs = args ?? throw new ArgumentNullException(nameof(args));
            return this;
        }

        public OmniSharpStdioServerConfiguration Build()
        {
            if (_processGetter != null)
                return new OmniSharpStdioServerConfiguration(_processGetter);
            else
            {
                if (!File.Exists(_executablePath))
                    throw new Exception($"OmniSharp server executable not found at path: {_executablePath}");
                
                return new OmniSharpStdioServerConfiguration(_executablePath!, _executableArgs!);
            }
        }
    }
}
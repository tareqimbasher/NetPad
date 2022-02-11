using System;
using System.Diagnostics;
using System.IO;

namespace NetPad.OmniSharpWrapper.Stdio
{
    public class OmniSharpStdioServerConfigurationBuilder
    {
        private Process? _process;
        private string? _executablePath;
        private string? _executableArgs;

        public OmniSharpStdioServerConfigurationBuilder UseExistingInstance(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
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
            if (_process != null)
                return new OmniSharpStdioServerConfiguration(_process);
            else
            {
                if (!File.Exists(_executablePath))
                    throw new Exception($"OmniSharp server executable not found at path: {_executablePath}");
                return new OmniSharpStdioServerConfiguration(_executablePath!, _executableArgs!);
            }
        }
    }
}
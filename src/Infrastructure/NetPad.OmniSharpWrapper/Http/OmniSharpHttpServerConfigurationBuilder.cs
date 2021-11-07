using System;
using System.IO;

namespace NetPad.OmniSharpWrapper.Http
{
    public class OmniSharpHttpServerConfigurationBuilder
    {
        private string? _uri;
        private string? _executablePath;
        private string? _executableArgs;

        public OmniSharpHttpServerConfigurationBuilder UseExistingInstance(string uri)
        {
            _uri = _uri = uri?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(uri));
            return this;
        }

        public OmniSharpHttpServerConfigurationBuilder UseNewProcess(string executablePath, string args)
        {
            _executablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            _executableArgs = args ?? throw new ArgumentNullException(nameof(args));
            return this;
        }

        public OmniSharpHttpServerConfiguration Build()
        {
            if (_uri != null)
                return new OmniSharpHttpServerConfiguration(_uri);
            else
            {
                if (!File.Exists(_executablePath))
                    throw new Exception($"OmniSharp server executable not found at path: {_executablePath}");
                return new OmniSharpHttpServerConfiguration(_executablePath!, _executableArgs!);
            }
        }
    }
}
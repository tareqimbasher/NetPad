using System;

namespace NetPad.OmniSharpWrapper.Http
{
    public class OmniSharpHttpServerConfiguration : OmniSharpServerConfiguration
    {
        public OmniSharpHttpServerConfiguration(string uri) : base(OmniSharpServerProtocolType.Http)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            ShouldCreateNewProcess = false;
        }
        
        public OmniSharpHttpServerConfiguration(string executablePath, string args) : base(OmniSharpServerProtocolType.Http)
        {
            ExecutablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            ExecutableArgs = args ?? throw new ArgumentNullException(nameof(args));
            ShouldCreateNewProcess = true;
        }
        
        public string? Uri { get; }
        public string? ExecutablePath { get; }
        public string? ExecutableArgs { get; }
        public bool ShouldCreateNewProcess { get; }
    }
}
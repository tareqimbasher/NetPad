using System;
using System.Diagnostics;

namespace NetPad.OmniSharpWrapper.Stdio
{
    public class OmniSharpStdioServerConfiguration : OmniSharpServerConfiguration
    {
        public OmniSharpStdioServerConfiguration(Process process) : base(OmniSharpServerProtocolType.Stdio)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            ShouldCreateNewProcess = false;
        }
        
        public OmniSharpStdioServerConfiguration(string executablePath, string args) : base(OmniSharpServerProtocolType.Stdio)
        {
            ExecutablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            ExecutableArgs = args ?? throw new ArgumentNullException(nameof(args));
            ShouldCreateNewProcess = true;
        }
        
        public Process? Process { get; }
        public string? ExecutablePath { get; }
        public string? ExecutableArgs { get; }
        public bool ShouldCreateNewProcess { get; }
    }
}
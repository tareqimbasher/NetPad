using System;
using System.Diagnostics;

namespace OmniSharp.Stdio
{
    internal class OmniSharpStdioServerConfiguration : OmniSharpServerConfiguration
    {
        public OmniSharpStdioServerConfiguration(Func<Process> processGetter) : base(OmniSharpServerProtocolType.Stdio)
        {
            ProcessGetter = processGetter ?? throw new ArgumentNullException(nameof(processGetter));
        }

        public OmniSharpStdioServerConfiguration(string executablePath, string args, OmniSharpDotNetSdk? dotNetSdk, string? workingDirectory) : base(OmniSharpServerProtocolType.Stdio)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            ExecutablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
            ExecutableArgs = dotNetSdk?.Version == null ? args : $"{args} Sdk:Version={dotNetSdk.Version}";
            DotNetSdk = dotNetSdk;
            WorkingDirectory = workingDirectory;
        }

        public Func<Process>? ProcessGetter { get; }
        public string? ExecutablePath { get; }
        public string? ExecutableArgs { get; }
        public bool ExternallyManagedProcess => ProcessGetter != null;
        public OmniSharpDotNetSdk? DotNetSdk { get; }
        public string? WorkingDirectory { get; }
    }
}

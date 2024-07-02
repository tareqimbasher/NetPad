using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Stdio;

namespace OmniSharp
{
    internal class DefaultOmniSharpFactory : IOmniSharpServerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultOmniSharpFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates an OmniSharp server that interacts via standard input/output and that spawns and manages a new OmniSharp process.
        /// </summary>
        /// <param name="executablePath">Path to the OmniSharp executable.</param>
        /// <param name="projectPath">An absolute path to a directory containing a project or a solution file.</param>
        /// <param name="additionalArgs">Additional arguments to pass to the OmniSharp executable. Separated by spaces.</param>
        /// <param name="dotNetSdkRootDirectoryPath">The root directory path for the .NET SDK installation.</param>
        /// <exception cref="ArgumentNullException">Thrown if required parameters are null.</exception>
        public IOmniSharpStdioServer CreateStdioServerFromNewProcess(
            string executablePath,
            string projectPath,
            string? additionalArgs,
            string? dotNetSdkRootDirectoryPath)
        {
            if (executablePath == null)
                throw new ArgumentNullException(nameof(executablePath));

            var projectArgs = "-s ";
            projectArgs += (projectPath ?? throw new ArgumentNullException(nameof(projectPath))).Trim();

            var args = $"{additionalArgs} {projectArgs}";

            var config = new OmniSharpStdioServerConfiguration(executablePath, args.Trim(), dotNetSdkRootDirectoryPath);

            var accessor = new OmniSharpServerStdioProcessAccessor(config);

            return new OmniSharpStdioServer(config, accessor, _serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        /// <summary>
        /// Creates an OmniSharp server that interacts via standard input/output that uses an OmniSharp process that is
        /// managed externally, ie. by the application. No new OmniSharp process will be spawned by calling this function,
        /// and no exising OmniSharp process will be stopped or restarted by calling this function.
        ///
        /// The process is expected to be started, stopped, and restarted by the application. The server started by calling
        /// this function will not attempt to control the process in any way.
        ///
        /// The process is expected to be started with the following values for its associated <see cref="ProcessStartInfo"/>:
        /// 1. RedirectStandardInput = true
        /// 2. RedirectStandardOutput = true
        /// 3. RedirectStandardError = true
        /// 4. UseShellExecute = false
        ///
        /// It is also expected that the application should call the following methods after starting the process:
        /// 1. Process.BeginOutputReadLine();
        /// 2. Process.BeginErrorReadLine();
        /// </summary>
        /// <param name="processGetter">A function that will be called to retrieve the application- managed process</param>
        public IOmniSharpStdioServer CreateStdioServerFromExistingProcess(Func<Process> processGetter)
        {
            var config = new OmniSharpStdioServerConfiguration(processGetter);

            var accessor = new OmniSharpServerStdioProcessAccessor(config);

            return new OmniSharpStdioServer(config, accessor, _serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}

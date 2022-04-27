using System;
using System.Diagnostics;

namespace OmniSharp
{
    /// <summary>
    /// Factory for creating an OmniSharp server.
    /// </summary>
    public interface IOmniSharpServerFactory
    {
        /// <summary>
        /// Creates a stdio OmniSharp server by spawning a new process.
        /// </summary>
        /// <param name="executablePath">The path to the OmniSharp executable.</param>
        /// <param name="projectPath">The path to the project file or directory.</param>
        /// <param name="additionalArgs">Additional arguments to be passed to the spawned OmniSharp process.</param>
        IOmniSharpServer CreateStdioServerFromNewProcess(
            string executablePath,
            string projectPath,
            string additionalArgs);

        /// <summary>
        /// Creates a stdio OmniSharp server from an existing process. Standard input and output must already be redirected.
        /// </summary>
        /// <param name="processGetter">A function that returns the existing process to use.</param>
        IOmniSharpServer CreateStdioServerFromExistingProcess(Func<Process> processGetter);
    }
}

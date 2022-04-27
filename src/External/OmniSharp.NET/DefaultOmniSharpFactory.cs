using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Stdio;

namespace OmniSharp
{
    public class DefaultOmniSharpFactory : IOmniSharpServerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultOmniSharpFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IOmniSharpServer CreateStdioServerFromNewProcess(string executablePath, string projectPath, string additionalArgs)
        {
            var args = "-s ";
            args += (projectPath ?? throw new ArgumentNullException(nameof(projectPath))).Trim();

            if (!string.IsNullOrWhiteSpace(additionalArgs))
                args += " " + additionalArgs;

            var config = new OmniSharpStdioServerConfiguration(executablePath, args.Trim());

            var accessor = new OmniSharpServerStdioProcessAccessor(config);

            return new OmniSharpStdioServer(config, accessor, _serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        public IOmniSharpServer CreateStdioServerFromExistingProcess(Func<Process> processGetter)
        {
            var config = new OmniSharpStdioServerConfiguration(processGetter);

            var accessor = new OmniSharpServerStdioProcessAccessor(config);

            return new OmniSharpStdioServer(config, accessor, _serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}

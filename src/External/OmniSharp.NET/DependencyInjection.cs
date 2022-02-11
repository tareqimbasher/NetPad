using System;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Stdio;
using OmniSharp.Utilities;

namespace OmniSharp
{
    public static class DependencyInjection
    {
        public static ServiceCollection AddStdioOmniSharpServer(this ServiceCollection services, Action<OmniSharpStdioServerConfigurationBuilder> configure)
        {
            var builder = new OmniSharpStdioServerConfigurationBuilder();

            configure(builder);

            var configuration = builder.Build();

            services.AddSingleton(configuration);
            services.AddSingleton<OmniSharpServerConfiguration>(configuration);
            services.AddSingleton<IOmniSharpServerProcessAccessor<ProcessIOHandler>, OmniSharpServerStdioProcessAccessor>();
            services.AddSingleton<IOmniSharpServer, OmniSharpStdioServer>();

            return services;
        }
    }
}
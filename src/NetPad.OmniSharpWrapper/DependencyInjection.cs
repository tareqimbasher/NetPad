using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetPad.OmniSharpWrapper.Http;
using NetPad.OmniSharpWrapper.Stdio;
using NetPad.OmniSharpWrapper.Utilities;

namespace NetPad.OmniSharpWrapper
{
    public static class DependencyInjection
    {
        public static ServiceCollection AddOmniSharpServer(this ServiceCollection services, Action<OmniSharpServerConfigurationBuilder> configure)
        {
            var builder = new OmniSharpServerConfigurationBuilder();
            configure(builder);

            var configuration = builder.Build();

            services.AddSingleton(configuration);

            if (configuration.ProtocolType == OmniSharpServerProtocolType.Stdio)
            {
                services.AddSingleton((OmniSharpStdioServerConfiguration)configuration);
                services.AddSingleton<IOmniSharpServerProcessAccessor<ProcessIOHandler>, OmniSharpServerStdioProcessAccessor>();
                services.AddSingleton<IOmniSharpServer, OmniSharpStdioServer>();
            }
            else if (configuration.ProtocolType == OmniSharpServerProtocolType.Http)
            {
                services.AddSingleton((OmniSharpHttpServerConfiguration)configuration);
                services.AddSingleton<IOmniSharpServerProcessAccessor<string>, OmniSharpServerHttpProcessAccessor>();
                services.AddSingleton<IOmniSharpServer, OmniSharpHttpServer>();
            }
            else
                throw new Exception($"Unknown protocol type: {configuration.ProtocolType}");

            return services;
        }
    }
}
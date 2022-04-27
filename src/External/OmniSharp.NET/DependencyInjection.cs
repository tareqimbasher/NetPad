using Microsoft.Extensions.DependencyInjection;

namespace OmniSharp
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers services for running OmniSharp server.
        /// </summary>
        public static IServiceCollection AddOmniSharpServer(this IServiceCollection services)
        {
            services.AddTransient<IOmniSharpServerFactory, DefaultOmniSharpFactory>();
            services.AddLogging();

            return services;
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Queries;
using NetPad.Sessions;
using OmniSharp;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace NetPad
{
    public class Startup
    {
        private readonly IApplicationLifetime _applicationLifetime;

        public Startup(IApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public void Configure()
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .Build();
            
            var services = new ServiceCollection();
            
            // Register foundational services
            services.AddSingleton(x => configuration);
            services.AddSingleton(x => (IClassicDesktopStyleApplicationLifetime)_applicationLifetime);
            services.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddConfiguration(configuration);
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Register application services
            services.AddSingleton<Settings>();
            services.AddSingleton<ISession, Session>();
            services.AddSingleton<IQueryManager, QueryManager>();
            // services.AddTransient<ITextEditingEngine, OmniSharpTextEditingEngine>();
            
            services.AddStdioOmniSharpServer(config =>
            {
                var cmd = "/home/tips/X/tmp/Omnisharp/omnisharp-linux-x64/run";

                var args = new List<string>
                {
                    // "-z", // Zero-based indicies
                    "-s", "/home/tips/Source/tmp/test-project",
                    "--hostPID", Process.GetCurrentProcess().Id.ToString(),
                    "DotNet:enablePackageRestore=false",
                    "--encoding", "utf-8",
                    //"--loglevel", "LogLevel.Debug", // replace with actual enum
                    "-v", // Sets --loglevel to debug
                };

                args.Add("RoslynExtensionsOptions:EnableImportCompletion=true");
                args.Add("RoslynExtensionsOptions:EnableAnalyzersSupport=true");
                args.Add("RoslynExtensionsOptions:EnableAsyncCompletion=true");
                
                config.UseNewInstance(cmd, string.Join(" ", args));
            });
            
            RegisterViewsAndViewModels(services);
            
            
            // services.AddSingleton<IViewLocator>(new ViewLocator());
            // Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
            // Locator.CurrentMutable.RegisterLazySingleton(() => new ViewLocator(), typeof(IViewLocator));
            
            services.UseMicrosoftDependencyResolver();
            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI();
        }
        
        private void RegisterViewsAndViewModels(IServiceCollection services)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };

            foreach (var assembly in assemblies)
            {
                var types = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                    .ToArray();
                
                var views = types.Where(t => typeof(IViewFor).IsAssignableFrom(t));
                var viewModels = types.Where(t => typeof(ReactiveObject).IsAssignableFrom(t));

                foreach (var view in views)
                {
                    services.AddSingleton(view);
                }

                foreach (var viewModel in viewModels)
                {
                    services.AddSingleton(viewModel);
                }
            }
        }
    }
}
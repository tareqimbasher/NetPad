using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Application;
using NetPad.Assemblies;
using NetPad.BackgroundServices;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Configuration;
using NetPad.CQs;
using NetPad.Events;
using NetPad.Middlewares;
using NetPad.Packages;
using NetPad.Plugins;
using NetPad.Resources;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.Sessions;
using NetPad.UiInterop;

namespace NetPad
{
    public partial class Startup
    {
        private readonly Assembly[] _pluginAssemblies =
        {
            typeof(NetPad.Plugins.OmniSharp.Plugin).Assembly
        };

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<HostInfo>();
            services.AddSingleton<Settings>(sp => sp.GetRequiredService<ISettingsRepository>().GetSettingsAsync().Result);
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<IAppStatusMessagePublisher, AppStatusMessagePublisher>();
            services.AddSingleton<ISession, Session>();
            services.AddSingleton<HttpClient>();
            services.AddTransient<ILogoService, LogoService>();

            // Repositories
            services.AddTransient<ISettingsRepository, FileSystemSettingsRepository>();
            services.AddTransient<IScriptRepository, FileSystemScriptRepository>();
            services.AddTransient<IAutoSaveScriptRepository, FileSystemAutoSaveScriptRepository>();

            // Script execution
            services.AddSingleton<IScriptNameGenerator, DefaultScriptNameGenerator>();
            services.AddTransient<IScriptEnvironmentFactory, DefaultScriptEnvironmentFactory>();
            services.AddTransient<ICodeParser, CSharpCodeParser>();
            services.AddTransient<ICodeCompiler, CSharpCodeCompiler>();
            services.AddTransient<IScriptRuntimeFactory, DefaultInMemoryScriptRuntimeFactory>();
            //services.AddTransient<IScriptRuntimeFactory, DefaultExternalProcessScriptRuntimeFactory>();
            services.AddTransient<IAssemblyLoader, UnloadableAssemblyLoader>();
            services.AddTransient<IAssemblyInfoReader, AssemblyInfoReader>();

            // Package management
            services.AddTransient<IPackageProvider, NuGetPackageProvider>();

            // Plugins
            var pluginInitialization = new PluginInitialization(Configuration, WebHostEnvironment);
            IPluginManager pluginManager = new PluginManager(pluginInitialization);
            services.AddSingleton(pluginInitialization);
            services.AddSingleton<IPluginManager>(pluginManager);

            // Hosted services
            services.AddHostedService<EventForwardToIpcBackgroundService>();
            services.AddHostedService<ScriptEnvironmentBackgroundService>();
            services.AddHostedService<ScriptDirectoryBackgroundService>();
            if (WebHostEnvironment.IsDevelopment())
            {
                //services.AddHostedService<DebugAssemblyUnloadBackgroundService>();
            }

            // Should be the last hosted service so it runs last on app start
            services.AddHostedService<AppSetupAndCleanupBackgroundService>();

            var pluginRegistrations = new List<PluginRegistration>();

            // Register plugins
            foreach (var pluginAssembly in _pluginAssemblies)
            {
                try
                {
                    var registration = pluginManager.RegisterPlugin(pluginAssembly, services);
                    pluginRegistrations.Add(registration);

                    Console.WriteLine("Registered plugin '{0}' from '{1}'",
                        registration.Plugin.Name,
                        registration.Assembly.FullName);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Could not register plugin: '{0}'. {1}", pluginAssembly.FullName, ex);
                }
            }

            // MVC
            var mvcBuilder = services.AddControllersWithViews()
                .AddJsonOptions(options => { JsonSerializer.Configure(options.JsonSerializerOptions); });

            foreach (var registration in pluginRegistrations)
            {
                mvcBuilder.AddApplicationPart(registration.Assembly);
            }

            // SignalR
            services.AddSignalR()
                .AddJsonProtocol(options => { JsonSerializer.Configure(options.PayloadSerializerOptions); });

            // In production, the SPA files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "App/dist"; });

            // Mediator
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatorRequestPipeline<,>));
            services.AddMediatR(new[] { typeof(Command).Assembly }.Union(pluginRegistrations.Select(pr => pr.Assembly)).ToArray());

            // Swagger
            if (WebHostEnvironment.IsDevelopment())
            {
                AddSwagger(services, pluginRegistrations);
            }

            // Allow ApplicationConfigurator to add/modify any service registrations it needs
            Program.ApplicationConfigurator.ConfigureServices(services);

            // We want to always use SignalR for IPC, overriding Electron's IPC service
            // This should come after the ApplicationConfigurator adds its services so it overrides it
            services.AddTransient<IIpcService, SignalRIpcService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var services = app.ApplicationServices;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }
            else
            {
                app.UseOpenApi();
                app.UseSwaggerUi3();
            }

            // Set host url
            var hostInfo = services.GetRequiredService<HostInfo>();
            hostInfo.SetWorkingDirectory(env.ContentRootPath);
            hostInfo.SetHostUrl(
                app.ServerFeatures
                    .Get<IServerAddressesFeature>()!
                    .Addresses
                    .First(a => a.StartsWith("http:")));

            // Add middlewares
            app.UseMiddleware<ExceptionHandlerMiddleware>();

            // Initialize plugins
            var pluginManager = app.ApplicationServices.GetRequiredService<IPluginManager>();
            pluginManager.ConfigurePlugins(app, env);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");

                endpoints.MapHub<IpcHub>("/ipc-hub");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "App";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:9000/");
                }
            });

            // Allow ApplicationConfigurator to run any configuration
            Program.ApplicationConfigurator.Configure(app, env);
        }
    }
}

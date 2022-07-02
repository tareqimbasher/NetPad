using System.Linq;
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
using NetPad.CQs;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.Middlewares;
using NetPad.Packages;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.Sessions;
using NetPad.UiInterop;
using NetPad.Utilities;
using OmniSharp;

namespace NetPad
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews()
                .AddJsonOptions(options => { JsonSerializer.Configure(options.JsonSerializerOptions); });

            services.AddSignalR()
                .AddJsonProtocol(options => { JsonSerializer.Configure(options.PayloadSerializerOptions); });

            // In production, the SPA files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "App/dist"; });

            services.AddMediatR(typeof(Command).Assembly);

            if (WebHostEnvironment.IsDevelopment())
            {
                AddSwagger(services);
            }

            services.AddSingleton<HostInfo>();
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<IAppStatusMessagePublisher, AppStatusMessagePublisher>();
            services.AddSingleton<ISession, Session>();

            // Repositories
            services.AddSingleton(sp => sp.GetRequiredService<ISettingsRepository>().GetSettingsAsync().Result);
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

            // OmniSharp
            services.AddSingleton<OmniSharpServerCatalog>();
            services.AddOmniSharpServer();

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

            // Allow ApplicationConfigurator to add any services it needs
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

            app.UseMiddleware<ExceptionHandlerMiddleware>();

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

            // Set host url
            services.GetRequiredService<HostInfo>().SetHostUrl(
                app.ServerFeatures
                    .Get<IServerAddressesFeature>()!
                    .Addresses
                    .First(a => a.StartsWith("http:")));

            // Allow ApplicationConfigurator to run any configuration
            Program.ApplicationConfigurator.Configure(app, env);
        }
    }
}

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Assemblies;
using NetPad.BackgroundServices;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.Middlewares;
using NetPad.Packages;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.UiInterop;

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

            services.AddSingleton<HostInfo>();
            services.AddSingleton(sp => sp.GetRequiredService<ISettingsRepository>().GetSettingsAsync().Result);

            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<ISession, Session>();
            services.AddTransient<ISettingsRepository, FileSystemSettingsRepository>();
            services.AddSingleton<IScriptRepository, FileSystemScriptRepository>();
            services.AddTransient<IScriptEnvironmentFactory, DefaultScriptEnvironmentFactory>();
            services.AddTransient<ICodeParser, CSharpCodeParser>();
            services.AddTransient<ICodeCompiler, CSharpCodeCompiler>();
            services.AddTransient<IAssemblyLoader, UnloadableAssemblyLoader>();
            services.AddTransient<IScriptRuntimeFactory, DefaultInMemoryScriptRuntimeFactory>();
            //services.AddTransient<IScriptRuntimeFactory, DefaultExternalProcessScriptRuntimeFactory>();
            services.AddTransient<IAssemblyInfoReader, AssemblyInfoReader>();
            services.AddTransient<IPackageProvider, NuGetPackageProvider>();

            services.AddHostedService<SessionBackgroundService>();
            services.AddHostedService<ScriptEnvironmentBackgroundService>();
            services.AddHostedService<ScriptDirectoryBackgroundService>();

            if (WebHostEnvironment.IsDevelopment())
            {
                AddSwagger(services);
                //services.AddHostedService<DebugAssemblyUnloadBackgroundService>();
            }

            Program.ApplicationConfigurator.ConfigureServices(services);

            // We want to always use SignalR for IPC, overriding Electron's IPC service
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

            services.GetRequiredService<HostInfo>().SetHostUrl(
                app.ServerFeatures
                    .Get<IServerAddressesFeature>()!
                    .Addresses
                    .First(a => a.StartsWith("http:")));

            Program.ApplicationConfigurator.Configure(app, env);
        }
    }
}

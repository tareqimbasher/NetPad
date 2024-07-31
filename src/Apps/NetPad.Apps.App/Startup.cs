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
using NetPad.Apps;
using NetPad.Apps.Configuration;
using NetPad.Apps.CQs;
using NetPad.Apps.Data;
using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.Apps.Plugins;
using NetPad.Apps.Resources;
using NetPad.Apps.Scripts;
using NetPad.Apps.UiInterop;
using NetPad.Assemblies;
using NetPad.BackgroundServices;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.ExecutionModel;
using NetPad.Middlewares;
using NetPad.Packages;
using NetPad.Packages.NuGet;
using NetPad.Plugins.OmniSharp;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.Sessions;
using NetPad.Swagger;

namespace NetPad;

public class Startup
{
    private readonly Assembly[] _pluginAssemblies =
    [
        typeof(Plugin).Assembly
    ];

    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        Configuration = configuration;
        WebHostEnvironment = webHostEnvironment;
        Console.WriteLine("Configuration:");
        Console.WriteLine($"   - .NET Runtime Version: {Environment.Version.ToString()}");
        Console.WriteLine($"   - Environment: {webHostEnvironment.EnvironmentName}");
        Console.WriteLine($"   - WebRootPath: {webHostEnvironment.WebRootPath}");
        Console.WriteLine($"   - ContentRootPath: {webHostEnvironment.ContentRootPath}");
        Console.WriteLine($"   - Shell: {Program.Shell.GetType().Name}");
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment WebHostEnvironment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCoreServices();

        services.AddSingleton<HostInfo>();
        services.AddTransient<ISettingsRepository, FileSystemSettingsRepository>();
        services.AddSingleton<Settings>(sp => sp.GetRequiredService<ISettingsRepository>().GetSettingsAsync().Result);
        services.AddSingleton<ISession, Session>();
        services.AddTransient<IAssemblyLoader, UnloadableAssemblyLoader>();
        services.AddTransient<ILogoService, LogoService>();
        services.AddSingleton<ITrivialDataStore, FileSystemTrivialDataStore>();
        services.AddTransient<IIpcService, SignalRIpcService>();

        // Script services
        services.AddTransient<ScriptService>();
        services.AddTransient<IScriptRepository, FileSystemScriptRepository>();
        services.AddTransient<IAutoSaveScriptRepository, FileSystemAutoSaveScriptRepository>();
        services.AddSingleton<IScriptNameGenerator, DefaultScriptNameGenerator>();

        // Script execution mechanism
        services.AddExternalExecutionModel(options =>
        {
            options.ProcessCliArgs = ["-html"];
            options.RedirectIo = true;
        });

        // Data connections
        services
            .AddDataConnectionFeature<
                FileSystemDataConnectionRepository,
                FileSystemDataConnectionResourcesRepository,
                FileSystemDataConnectionResourcesCache>()
            .AddEntityFrameworkCoreDataConnectionDriver();

        // Package management
        services.AddTransient<IPackageProvider, NuGetPackageProvider>();

        // Plugins
        var pluginInitialization = new PluginInitialization(Configuration, WebHostEnvironment);
        IPluginManager pluginManager = new PluginManager(pluginInitialization);
        services.AddSingleton(pluginInitialization);
        services.AddSingleton(pluginManager);

        // Hosted services
        services.AddHostedService<EventHandlerBackgroundService>();
        services.AddHostedService<EventForwardToIpcBackgroundService>();
        services.AddHostedService<ScriptEnvironmentBackgroundService>();
        services.AddHostedService<ScriptsFileWatcherBackgroundService>();

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

        // HttpClient
        services.AddSingleton<HttpClient>(_ =>
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(1);
            return httpClient;
        });

        // SignalR
        services.AddSignalR()
            .AddJsonProtocol(options => { JsonSerializer.Configure(options.PayloadSerializerOptions); });

        // In production, the SPA files will be served from this directory
        services.AddSpaStaticFiles(configuration => { configuration.RootPath = "App/dist"; });

        // Mediator
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatorRequestPipeline<,>));
        services.AddMediatR(new[] { typeof(Command).Assembly }.Union(pluginRegistrations.Select(pr => pr.Assembly))
            .ToArray());

        // Swagger
#if DEBUG
        SwaggerSetup.AddSwagger(services, WebHostEnvironment, pluginRegistrations);
#endif

        // Allow Shell to add/modify any service registrations it needs
        Program.Shell.ConfigureServices(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var services = app.ApplicationServices;

#if DEBUG
        app.UseDeveloperExceptionPage();
#else
        app.UseExceptionHandler("/Error");
        app.UseHsts();
#endif

        //app.UseHttpsRedirection();
        app.UseStaticFiles();

#if DEBUG
        app.UseOpenApi();
        app.UseSwaggerUi3();
#else
        app.UseSpaStaticFiles();
#endif

        // Set host url
        var hostInfo = services.GetRequiredService<HostInfo>();
        hostInfo.SetWorkingDirectory(env.ContentRootPath);

        var serverAddresses = app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;

        if (serverAddresses == null || !serverAddresses.Any())
        {
            throw new Exception("No server urls specified. Specify the url with the '--urls' parameter");
        }

        var url = serverAddresses.FirstOrDefault(a => a.StartsWith("https:")) ??
                  serverAddresses.FirstOrDefault(a => a.StartsWith("http:"));

        if (url == null)
        {
            throw new Exception("No server urls specified that start with 'http' or 'https'");
        }

        hostInfo.SetHostUrl(url);

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
            spa.Options.SourcePath = "App";
#if DEBUG
            spa.UseProxyToSpaDevelopmentServer("http://localhost:9000/");
#endif
        });

        Program.Shell.Initialize(app, env);
    }
}

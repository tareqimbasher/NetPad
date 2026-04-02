using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps;
using NetPad.Apps.CQs;
using NetPad.Apps.Security;
using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.Apps.Plugins;
using NetPad.Apps.Resources;
using NetPad.Apps.UiInterop;
using NetPad.BackgroundServices;
using NetPad.Common;
using NetPad.ExecutionModel;
using NetPad.Host.Middlewares;
using NetPad.Host.Swagger;
using NetPad.Plugins.OmniSharp;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.Services.UiInterop;
using ExceptionHandlerMiddleware = NetPad.Host.Middlewares.ExceptionHandlerMiddleware;

namespace NetPad;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    // Add built-in plugins here.
    private readonly Assembly[] _pluginAssemblies =
    [
        typeof(OmniSharpPlugin).Assembly
    ];

    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
        Console.WriteLine("Configuration:");
        Console.WriteLine($"   - NetPad Version: {Application.AppIdentifier.PRODUCT_VERSION}");
        Console.WriteLine($"   - .NET Runtime Version: {Environment.Version.ToString()}");
        Console.WriteLine($"   - Environment: {webHostEnvironment.EnvironmentName}");
        Console.WriteLine($"   - WebRootPath: {webHostEnvironment.WebRootPath}");
        Console.WriteLine($"   - ContentRootPath: {webHostEnvironment.ContentRootPath}");
        Console.WriteLine($"   - Shell: {Program.Shell?.GetType().Name}");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCoreServices();

        // Application services
        services.AddSingleton<SecurityToken>();
        services.AddSingleton<HostInfo>();
        services.AddTransient<ILogoService, LogoService>();
        services.AddTransient<IIpcService, SignalRIpcService>();
        services.AddTransient<ScriptService>();
        services.AddTransient<IAutoSaveScriptRepository, FileSystemAutoSaveScriptRepository>();

        // Script execution mechanism
        services.AddClientServerExecutionModel();

        // Headless execution (for MCP server and API consumers)
        services.AddTransient<HeadlessScriptRunnerFactory>();
        services.AddTransient<HeadlessScriptExecutionService>();
        services.AddSingleton<ScriptOutputCaptureService>();

        // Data connections
        services
            .AddDataConnectionFeature()
            .AddEntityFrameworkCoreDataConnectionDriver();

        // Plugins
        var pluginInitialization = new PluginInitialization(_configuration, _webHostEnvironment);
        IPluginManager pluginManager = new PluginManager(pluginInitialization);
        services.AddSingleton(pluginInitialization);
        services.AddSingleton(pluginManager);

        // Register plugins
        var pluginRegistrations = new List<PluginRegistration>();
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

        // Hosted background services
        services.AddHostedService<EventHandlerBackgroundService>();
        services.AddHostedService<EventForwardToIpcBackgroundService>();
        services.AddHostedService<ScriptEnvironmentBackgroundService>();
        services.AddHostedService<ScriptsFileWatcherBackgroundService>();
        // Should be the last hosted service so it runs last on app start
        services.AddHostedService<AppSetupAndCleanupBackgroundService>();

        // MVC
        var mvcBuilder = services.AddControllersWithViews()
            .AddJsonOptions(options => { JsonSerializer.Configure(options.JsonSerializerOptions); });

        foreach (var registration in pluginRegistrations)
        {
            mvcBuilder.AddApplicationPart(registration.Assembly);
        }

        // HttpClient
        services.AddHttpClient(string.Empty, httpClient =>
        {
            httpClient.Timeout = TimeSpan.FromMinutes(1);
        });

        // SignalR
        services.AddSignalR()
            .AddJsonProtocol(options => { JsonSerializer.Configure(options.PayloadSerializerOptions); });

        // In production, the SPA files will be served from this directory
        services.AddSpaStaticFiles(configuration => { configuration.RootPath = "App/dist"; });

        // Mediator
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatorRequestPipeline<,>));
        services.AddMediatR(
            new[] { typeof(Command).Assembly }
                .Concat(pluginRegistrations.Select(pr => pr.Assembly))
                .ToArray()
        );

        // Swagger
#if DEBUG
        SwaggerSetup.AddSwagger(services, _webHostEnvironment, pluginRegistrations);
#endif

        // Allow Shell to add/modify any service registrations it needs.
        Program.Shell?.ConfigureServices(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Using DEBUG pre-processor symbol instead of checking environment. Reason is some users set
        // DOTNET_ENVIRONMENT (or similar variables) to "Development" globally, which breaks app in production.

        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseStaticFiles();

#if !DEBUG
        app.UseSpaStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            }
        });
#endif

        app.UseMiddleware<TokenValidationMiddleware>();

#if DEBUG
        app.UseOpenApi();
        app.UseSwaggerUi();
#endif

        InitializeHostInfo(app, env);

        // Initialize plugins
        var pluginManager = app.ApplicationServices.GetRequiredService<IPluginManager>();
        pluginManager.ConfigurePlugins(app, env);

        app.UseRouting();

        Program.Shell?.ConfigureRequestPipeline(app, env);

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
            spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            };
#if DEBUG
            spa.UseProxyToSpaDevelopmentServer("http://localhost:9000/");
#endif
        });

        Program.Shell?.Initialize(app, env);
    }

    private static void InitializeHostInfo(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var hostInfo = app.ApplicationServices.GetRequiredService<HostInfo>();
        hostInfo.SetWorkingDirectory(env.ContentRootPath);

        var serverAddresses = app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;

        if (serverAddresses == null || serverAddresses.Count == 0)
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

        var securityToken = app.ApplicationServices.GetRequiredService<SecurityToken>();
        ConnectionFileManager.Write(url, securityToken.Token, Program.Shell?.GetType().Name);
    }
}

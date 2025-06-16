using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps;
using NetPad.Apps.Plugins;
using NetPad.Common;
using NetPad.Plugins.OmniSharp;

namespace NetPad.Swagger;

public class SwaggerCodeGenerationStartup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
{
    private readonly Assembly[] _pluginAssemblies =
    [
        typeof(OmniSharpPlugin).Assembly
    ];

    public IConfiguration Configuration { get; } = configuration;
    public IWebHostEnvironment WebHostEnvironment { get; } = webHostEnvironment;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCoreServices();

        services.AddSingleton<HostInfo>();

        // Plugins
        var pluginInitialization = new PluginInitialization(Configuration, WebHostEnvironment);
        IPluginManager pluginManager = new PluginManager(pluginInitialization);
        services.AddSingleton(pluginInitialization);
        services.AddSingleton(pluginManager);

        var pluginRegistrations = new List<PluginRegistration>();

        // Register plugins
        var emptyServices = new ServiceCollection(); // We don't care about plugin service registrations in this scenario
        foreach (var pluginAssembly in _pluginAssemblies)
        {
            try
            {
                var registration = pluginManager.RegisterPlugin(pluginAssembly, emptyServices);
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
            .AddJsonOptions(options => JsonSerializer.Configure(options.JsonSerializerOptions));

        foreach (var registration in pluginRegistrations)
        {
            mvcBuilder.AddApplicationPart(registration.Assembly);
        }

        SwaggerSetup.AddSwagger(services, WebHostEnvironment, pluginRegistrations);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var services = app.ApplicationServices;
        app.UseDeveloperExceptionPage();

        app.UseOpenApi();
        app.UseSwaggerUi3();

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
    }
}

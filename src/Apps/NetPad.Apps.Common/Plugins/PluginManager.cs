using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad.Apps.Plugins;

public class PluginManager(PluginInitialization pluginInitialization) : IPluginManager
{
    private readonly Dictionary<string, PluginRegistration> _pluginRegistrations = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<PluginRegistration> PluginRegistrations => _pluginRegistrations.Values;

    public PluginRegistration RegisterPlugin(Assembly assembly, IServiceCollection services)
    {
        var pluginTypes = assembly.GetExportedTypes().Where(t => t.IsClass && typeof(IPlugin).IsAssignableFrom(t)).ToArray();

        if (pluginTypes.Length == 0)
        {
            throw new Exception($"Assembly {assembly.GetName()} does not have a public class that " +
                                $"implements the {typeof(IPlugin).FullName} interface.");
        }

        if (pluginTypes.Length > 1)
        {
            throw new Exception($"Assembly {assembly.GetName()} has {pluginTypes.Length} public classes that " +
                                $"implement the {typeof(IPlugin).FullName} interface. Expected only 1.");
        }

        var pluginType = pluginTypes.Single();

        var targetCtor = pluginType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var args = c.GetParameters();
                return args.Length == 1 && args.Single().ParameterType == typeof(PluginInitialization);
            });

        if (targetCtor == null)
        {
            throw new Exception($"Could not find a public constructor in type {pluginType.FullName} that has " +
                                $"a single parameter of type {nameof(PluginInitialization)}.");
        }

        if (targetCtor.Invoke([pluginInitialization]) is not IPlugin plugin)
        {
            throw new Exception($"Could not construct an instance of type {pluginType.FullName}.");
        }

        if (string.IsNullOrWhiteSpace(plugin.Id))
        {
            throw new Exception($"Plugin '{assembly.FullName}' has an empty ID.");
        }

        if (plugin.Id.Contains(' '))
        {
            throw new Exception($"Plugin '{assembly.FullName}' has an ID with a space. Plugin IDs cannot contain spaces.");
        }

        if (_pluginRegistrations.ContainsKey(plugin.Id))
        {
            throw new Exception($"Plugin from assembly '{assembly.FullName}' has ID '{plugin.Id}', " +
                                "but another plugin is already registered with this ID.");
        }

        if (string.IsNullOrWhiteSpace(plugin.Name))
        {
            throw new Exception($"Plugin '{assembly.FullName}' has an empty Name.");
        }

        plugin.ConfigureServices(services);

        var registration = new PluginRegistration(assembly, plugin);

        _pluginRegistrations.Add(plugin.Id, registration);

        services.AddSingleton(typeof(IPlugin), plugin);

        return registration;
    }

    public void ConfigurePlugins(IApplicationBuilder app, IHostEnvironment env)
    {
        foreach (var plugin in app.ApplicationServices.GetServices<IPlugin>())
        {
            plugin.Configure(app, env);
        }
    }
}

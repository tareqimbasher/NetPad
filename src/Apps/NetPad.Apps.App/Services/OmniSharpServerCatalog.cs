using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Utilities;
using OmniSharp;

namespace NetPad.Services;

/// <summary>
/// Manages and keeps a collection of created OmniSharp servers.
/// </summary>
public class OmniSharpServerCatalog
{
    private readonly IServiceScope _serviceScope;
    private readonly ILogger<OmniSharpServerCatalog> _logger;
    private readonly Dictionary<Guid, CatalogItem> _items;

    public OmniSharpServerCatalog(IServiceProvider serviceProvider, ILogger<OmniSharpServerCatalog> logger)
    {
        _serviceScope = serviceProvider.CreateScope();
        _logger = logger;
        _items = new Dictionary<Guid, CatalogItem>();
    }

    public AppOmniSharpServer? GetOmniSharpServer(Guid scriptId)
    {
        if (_items.TryGetValue(scriptId, out var item))
            return item.AppOmniSharpServer;

        // This can occur if omnisharp server is still initializing/starting and is not yet ready
        return null;
    }

    public async Task StartOmniSharpServerAsync(ScriptEnvironment environment)
    {
        if (_items.ContainsKey(environment.Script.Id))
        {
            throw new InvalidOperationException($"An OmniSharp server already exists for script ID: {environment.Script.Id}");
        }

        var serviceScope = _serviceScope.ServiceProvider.CreateScope();

        var settings = serviceScope.ServiceProvider.GetRequiredService<Settings>();

        if (settings.EditorOptions.CodeCompletion.Provider is not OmniSharpCodeCompletionProviderOptions omniSharpCodeCompletionProviderOptions)
        {
            _logger.LogError("Code completion provider must be of type {ExpectedType}", nameof(OmniSharpCodeCompletionProviderOptions));
            return;
        }

        var server = new AppOmniSharpServer(
            environment,
            serviceScope.ServiceProvider.GetRequiredService<IOmniSharpServerFactory>(),
            serviceScope.ServiceProvider.GetRequiredService<ICodeParser>(),
            serviceScope.ServiceProvider.GetRequiredService<IEventBus>(),
            omniSharpCodeCompletionProviderOptions,
            serviceScope.ServiceProvider.GetRequiredService<ILogger<AppOmniSharpServer>>(),
            serviceScope.ServiceProvider.GetRequiredService<ILogger<ScriptProject>>()
        );

        _logger.LogDebug("Initialized a new {Type} for script {Script}",
            nameof(AppOmniSharpServer),
            environment.Script);

        try
        {
            bool started = await server.StartAsync();

            _logger.LogDebug("Attempted to start {Type}. Succeeded: {Success}",
                nameof(AppOmniSharpServer),
                started);

            if (started)
            {
                _items.Add(environment.Script.Id, new CatalogItem(environment.Script.Id, server, serviceScope));
                _logger.LogDebug("Added OmniSharp server for script {Script}", environment.Script);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred starting OmniSharp server");
        }
    }

    public async Task StopOmniSharpServerAsync(ScriptEnvironment environment)
    {
        _logger.LogDebug("Finding OmniSharp server to stop for script {Script}", environment.Script);

        // Continuously try to find an OmniSharp server for the script for a few seconds.
        // An OmniSharp server could still be starting and so we want to do multiple checks to ensure
        // we find it if it was slow to start.
        CatalogItem? item = null;
        int findCounter = 0;

        while (++findCounter <= 10)
        {
            if (_items.TryGetValue(environment.Script.Id, out item))
            {
                _logger.LogDebug("Found OmniSharp server to stop for script {Script} on attempt {Attempt}",
                    environment.Script,
                    findCounter);
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        if (item == null)
        {
            _logger.LogDebug("No OmniSharp server found for script {Script} after {Attempts} attempts",
                environment.Script,
                findCounter);
            return;
        }

        _items.Remove(environment.Script.Id);

        try
        {
            var server = item.AppOmniSharpServer;
            await Retry.ExecuteAsync(5, TimeSpan.FromSeconds(1), async () => { await server.StopAsync(); });
            _logger.LogDebug("Stopped OmniSharp server for script {Script}", environment.Script);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping OmniSharp server for script {Script}", environment.Script);
        }
        finally
        {
            item.ServiceScope.Dispose();
        }
    }

    private class CatalogItem
    {
        public CatalogItem(Guid scriptId, AppOmniSharpServer appOmniSharpServer, IServiceScope serviceScope)
        {
            ScriptId = scriptId;
            AppOmniSharpServer = appOmniSharpServer;
            ServiceScope = serviceScope;
        }

        public Guid ScriptId { get; }
        public AppOmniSharpServer AppOmniSharpServer { get; }
        public IServiceScope ServiceScope { get; }
    }
}

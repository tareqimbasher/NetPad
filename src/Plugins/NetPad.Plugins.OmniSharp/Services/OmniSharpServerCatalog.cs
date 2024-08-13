using NetPad.Application;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Utilities;
using OmniSharp;

namespace NetPad.Plugins.OmniSharp.Services;

/// <summary>
/// Manages and keeps a collection of created OmniSharp servers.
/// </summary>
public class OmniSharpServerCatalog(
    IServiceProvider serviceProvider,
    IAppStatusMessagePublisher appStatusMessagePublisher,
    ILogger<OmniSharpServerCatalog> logger)
{
    private readonly IServiceScope _serviceScope = serviceProvider.CreateScope();
    private readonly Dictionary<Guid, CatalogItem> _items = new();

    public bool HasOmniSharpServer(Guid scriptId)
    {
        return _items.ContainsKey(scriptId);
    }

    public async Task<AppOmniSharpServer?> GetOmniSharpServerAsync(Guid scriptId)
    {
        if (_items.TryGetValue(scriptId, out var item))
            return await item.AppOmniSharpServerTask;

        // This can occur if omnisharp server is still initializing/starting and is not yet ready
        // or if it failed to start
        return null;
    }

    public async Task StartOmniSharpServerAsync(ScriptEnvironment environment)
    {
        if (_items.ContainsKey(environment.Script.Id))
        {
            throw new InvalidOperationException(
                $"An OmniSharp server already exists for script ID: {environment.Script.Id}");
        }

        var serviceScope = _serviceScope.ServiceProvider.CreateScope();

        var server = new AppOmniSharpServer(
            environment,
            serviceScope.ServiceProvider.GetRequiredService<OmniSharpServerBuilder>(),
            serviceScope.ServiceProvider.GetRequiredService<IDataConnectionResourcesCache>(),
            serviceScope.ServiceProvider.GetRequiredService<Settings>(),
            serviceScope.ServiceProvider.GetRequiredService<ICodeParser>(),
            serviceScope.ServiceProvider.GetRequiredService<IEventBus>(),
            serviceScope.ServiceProvider.GetRequiredService<IDotNetInfo>(),
            serviceScope.ServiceProvider.GetRequiredService<ILogger<AppOmniSharpServer>>(),
            serviceScope.ServiceProvider.GetRequiredService<ILogger<OmniSharpProject>>()
        );

        logger.LogDebug("Initialized a new {Type} for script {Script}",
            nameof(AppOmniSharpServer),
            environment.Script);

        try
        {
            await appStatusMessagePublisher.PublishAsync(environment.Script.Id, "Starting OmniSharp Server...");
            var startTask = server.StartAsync();

            // We don't want to await
#pragma warning disable CS4014
            startTask.ContinueWith(async task =>
#pragma warning restore CS4014
            {
                bool started = task.Status == TaskStatus.RanToCompletion && task.Result;

                logger.LogDebug("Attempted to start {Type}. Succeeded: {Success}",
                    nameof(AppOmniSharpServer),
                    started);

                await appStatusMessagePublisher.PublishAsync(
                    environment.Script.Id,
                    $"OmniSharp Server {(started ? "started" : "failed to start")}",
                    started ? AppStatusMessagePriority.Normal : AppStatusMessagePriority.High);

                if (!started)
                {
                    _items.Remove(environment.Script.Id);
                    serviceScope.Dispose();
                }
            });

            Task<AppOmniSharpServer?> serverTask = Task.Run(async () =>
            {
                try
                {
                    await startTask;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred starting OmniSharp server");
                    await appStatusMessagePublisher.PublishAsync(
                        environment.Script.Id,
                        "OmniSharp Server failed to start",
                        AppStatusMessagePriority.High,
                        true);
                    return null;
                }

                return server;
            });

            _items.Add(environment.Script.Id, new CatalogItem(serverTask, serviceScope));
            logger.LogDebug("Added OmniSharp server for script {Script}", environment.Script);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while attempting to start OmniSharp server");
            await appStatusMessagePublisher.PublishAsync(
                environment.Script.Id,
                "OmniSharp Server failed to start",
                AppStatusMessagePriority.High,
                true);
        }
    }

    public async Task StopOmniSharpServerAsync(ScriptEnvironment environment)
    {
        logger.LogDebug("Finding OmniSharp server to stop for script {Script}", environment.Script);

        // Continuously try to find an OmniSharp server for the script for a few seconds.
        // A call to stop an OmniSharp server could be fired before the call to start it was fired
        // so we want to do multiple checks to ensure we find it if it starts later.
        CatalogItem? item = null;
        int findCounter = 0;

        while (++findCounter <= 3)
        {
            if (_items.TryGetValue(environment.Script.Id, out item))
            {
                logger.LogDebug("Found OmniSharp server to stop for script {Script} on attempt {Attempt}",
                    environment.Script,
                    findCounter);
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        if (item == null)
        {
            logger.LogDebug("No OmniSharp server found for script {Script} after {Attempts} attempts",
                environment.Script,
                findCounter);
            return;
        }

        _items.Remove(environment.Script.Id);

        try
        {
            var server = await item.AppOmniSharpServerTask;

            if (server != null)
            {
                await Retry.ExecuteAsync(5, TimeSpan.FromSeconds(1), async () => { await server.StopAsync(); });
                logger.LogDebug("Stopped OmniSharp server for script {Script}", environment.Script);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping OmniSharp server for script {Script}", environment.Script);
        }
        finally
        {
            item.ServiceScope.Dispose();
        }
    }

    private class CatalogItem(Task<AppOmniSharpServer?> appOmniSharpServerTask, IServiceScope serviceScope)
    {
        public Task<AppOmniSharpServer?> AppOmniSharpServerTask { get; } = appOmniSharpServerTask;
        public IServiceScope ServiceScope { get; } = serviceScope;
    }
}

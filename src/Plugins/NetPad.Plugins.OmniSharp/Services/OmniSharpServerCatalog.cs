using System.Collections.Concurrent;
using System.ComponentModel;
using NetPad.Application;
using NetPad.Scripts;
using NetPad.Utilities;

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
    private readonly ConcurrentDictionary<Guid, CatalogItem> _items = new();

    public bool HasOmniSharpServer(Guid scriptId)
    {
        return _items.ContainsKey(scriptId);
    }

    public async Task<AppOmniSharpServer?> GetOmniSharpServerAsync(Guid scriptId)
    {
        if (_items.TryGetValue(scriptId, out var item))
        {
            return await item.AppOmniSharpServerTask;
        }

        // This can occur if omnisharp server is still initializing/starting and is not yet ready
        // or if it failed to start
        return null;
    }

    public Task StartOmniSharpServerAsync(ScriptEnvironment environment)
    {
        if (_items.ContainsKey(environment.Script.Id))
        {
            throw new InvalidOperationException(
                $"An OmniSharp server already exists for script ID: {environment.Script.Id}");
        }

        logger.LogDebug("Initialized a new {Type} for script {Script}",
            nameof(AppOmniSharpServer),
            environment.Script);

        _ = appStatusMessagePublisher.PublishAsync(environment.Script.Id, "Starting OmniSharp server...");

        var catalogItem = _items.GetOrAdd(
            environment.Script.Id,
            static (id, ctx) => CreateCatalogItem(ctx.Item1, ctx.Item2, ctx.Item3),
            (_serviceScope.ServiceProvider.CreateScope(), environment, logger)
        );

        logger.LogDebug("Added OmniSharp server for script {Script}", environment.Script);

        var serverTask = catalogItem.AppOmniSharpServerTask;

        serverTask.ContinueWith(async t =>
        {
            if (t.IsFaulted || t is { IsCompletedSuccessfully: true, Result: null })
            {
                _items.TryRemove(environment.Script.Id, out _);

                await appStatusMessagePublisher.PublishAsync(
                    environment.Script.Id,
                    "OmniSharp server failed to start. Check log file for details.",
                    AppStatusMessagePriority.High,
                    true);
            }
            else if (t.IsCompletedSuccessfully)
            {
                await appStatusMessagePublisher.PublishAsync(environment.Script.Id, "OmniSharp server started");
            }
        }, TaskContinuationOptions.ExecuteSynchronously);

        return serverTask;
    }

    public async Task StopOmniSharpServerAsync(ScriptEnvironment environment)
    {
        logger.LogDebug("Finding OmniSharp server to stop for script {Script}", environment.Script);

        // Try to find an OmniSharp server for the script for a few seconds.
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

        _items.TryRemove(environment.Script.Id, out _);

        try
        {
            var server = await item.AppOmniSharpServerTask;

            if (server != null)
            {
                await Retry.ExecuteAsync(5, TimeSpan.FromSeconds(1), async () => await server.StopAsync());
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

    private static CatalogItem CreateCatalogItem(
        IServiceScope serviceScope,
        ScriptEnvironment environment,
        ILogger logger)
    {
        var serverTask = Task.Run(async () =>
        {
            AppOmniSharpServer? server;

            try
            {
                server = ActivatorUtilities.CreateInstance<AppOmniSharpServer>(serviceScope.ServiceProvider,
                    environment);

                try
                {
                    await server.StartAsync();
                }
                catch (Win32Exception ex) when (ex.Message.ContainsIgnoreCase("Permission denied"))
                {
                    var locator = serviceScope.ServiceProvider.GetRequiredService<IOmniSharpServerLocator>();
                    if (await AttemptFixExecutablePermissions(locator, logger))
                    {
                        await server.StartAsync();
                    }
                    else
                    {
                        logger.LogError("Could not fix executable permissions");
                        throw;
                    }
                }

                if (!server.OmniSharpServer.IsProcessRunning())
                {
                    throw new Exception("OmniSharp server was started but the process is not running");
                }

                logger.LogDebug("Started OmniSharp server");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while starting OmniSharp server");
                serviceScope.Dispose();
                return null;
            }

            return server;
        });

        return new CatalogItem(serverTask, serviceScope);
    }

    private static async Task<bool> AttemptFixExecutablePermissions(IOmniSharpServerLocator locator, ILogger logger)
    {
        if (PlatformUtil.IsOSWindows())
        {
            logger.LogError("Cannot fix OmniSharp executable permissions on Windows");
            return false;
        }

        var location = await locator.GetServerLocationAsync();
        if (location == null)
        {
            logger.LogError("Could not get OmniSharp executable location");
            return false;
        }

        if (!ProcessUtil.SetUnixExecutablePermission(location.ExecutablePath))
        {
            logger.LogError(
                "Could not set executable flag on downloaded OmniSharp executable: {ExecutablePath}",
                location.ExecutablePath);
            return false;
        }

        return true;
    }

    private class CatalogItem(Task<AppOmniSharpServer?> appOmniSharpServerTask, IServiceScope serviceScope)
    {
        public Task<AppOmniSharpServer?> AppOmniSharpServerTask { get; } = appOmniSharpServerTask;
        public IServiceScope ServiceScope { get; } = serviceScope;
    }
}

using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using NetPad.DotNet;
using NetPad.ExecutionModel.ClientServer.Messages;
using NetPad.IO;
using NetPad.Scripts;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.ExecutionModel.ClientServer.ScriptHost;

public class ScriptHostProcessManager(
    Script script,
    FilePath scriptHostExecutablePath,
    DirectoryPath scriptHostRootDir,
    Action<string> nonMessageOutputHandler,
    Action<string> errorOutputHandler,
    ILogger logger,
    Action<ScriptHostIpcGateway> addMessageHandlers)
{
    private static readonly SemaphoreSlim _scriptHostProcessStartLock = new(1, 1);
    private Process? _scriptHostProcess;
    private ScriptHostIpcGateway? _ipcGateway;
    private uint _sendIpcMessageSeq;

    private readonly Channel<ScriptHostIpcMessage> _sendQueue = Channel.CreateUnbounded<ScriptHostIpcMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = true
    });

    public ScriptHostIpcGateway IpcGateway =>
        _ipcGateway ?? throw new InvalidOperationException("IpcGateway is not initialized.");

    public bool IsScriptHostRunning() => _scriptHostProcess?.IsProcessRunning() == true;

    public void RunScript(
        Guid runId,
        DirectoryPath scriptHostDepsDir,
        DirectoryPath scriptDir,
        FilePath scriptAssemblyPath,
        string[] probingPaths)
    {
        EnsureScriptHostStarted();

        var message = new RunScriptMessage(
            runId,
            script.Id,
            script.Name,
            script.Path,
            script.IsDirty,
            scriptHostDepsDir.Path,
            scriptDir.Path,
            scriptAssemblyPath.Path,
            probingPaths
        );

        Send(message);
    }

    public void Send<T>(T message) where T : class
    {
        logger.LogDebug("Sending message: {MessageType}", typeof(T).Name);

        _sendQueue.Writer.TryWrite(new ScriptHostIpcMessage(
            Interlocked.Increment(ref _sendIpcMessageSeq),
            typeof(T).FullName ??
            throw new InvalidOperationException($"Expected type '{typeof(T).Name}' to have a FullName"),
            JsonSerializer.Serialize(message)
        ));
    }

    private async Task ProcessSendQueueAsync()
    {
        var reader = _sendQueue.Reader;
        while (_ipcGateway != null && await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var message))
            {
                _ipcGateway.Send(message);
            }
        }
    }

    public void StopScriptHost()
    {
        logger.LogDebug("Stopping script-host");
        _scriptHostProcessStartLock.Wait();
        try
        {
            Cleanup();
        }
        finally
        {
            _scriptHostProcessStartLock.Release();
        }
    }

    private void EnsureScriptHostStarted()
    {
        if (_scriptHostProcess != null)
        {
            if (_scriptHostProcess.IsProcessRunning())
            {
                return;
            }

            Cleanup();
        }

        _scriptHostProcessStartLock.Wait();
        logger.LogDebug("Starting script-host process");

        try
        {
            UpdateScriptHostRuntimeConfig();

            var startInfo = new ProcessStartInfo(
                    scriptHostExecutablePath.Path,
                    $"--parent {Environment.ProcessId}")
                .CopyCurrentEnvironmentVariables()
                .WithRedirectIO()
                .WithNoUi();

            // On Windows, we need this environment var to force console output when using the ConsoleLoggingProvider
            // See: https://github.com/dotnet/runtime/blob/8a2e7e3e979d671d97cb408fbcbdbee5594479a4/src/libraries/Microsoft.Extensions.Logging.Console/src/ConsoleLoggerProvider.cs#L69
            if (script.Config.UseAspNet && PlatformUtil.IsOSWindows())
            {
                startInfo.EnvironmentVariables.Add("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "true");
            }

            _scriptHostProcess = Process.Start(startInfo) ?? throw new Exception("Could not start script-host process");
            _ipcGateway = new ScriptHostIpcGateway(_scriptHostProcess.StandardInput, logger);

            _ipcGateway.On<ScriptHostReadyMessage>(msg =>
            {
                logger.LogDebug("script-host is ready");
                _ = ProcessSendQueueAsync();
            });

            addMessageHandlers(_ipcGateway);

            _ipcGateway.Listen(
                _scriptHostProcess.StandardOutput,
                nonMessageOutputHandler);

            _scriptHostProcess.ErrorDataReceived += (_, ev) =>
            {
                if (ev.Data != null)
                {
                    errorOutputHandler(ev.Data);
                }
            };
            _scriptHostProcess.BeginErrorReadLine();

            _scriptHostProcess.Exited += (_, _) =>
            {
                logger.LogDebug("script-host process exited");
                Cleanup();
                _ipcGateway.Handle(new ScriptHostExitedMessage());
            };

            _ = _scriptHostProcess.WaitForExitAsync();
        }
        finally
        {
            _scriptHostProcessStartLock.Release();
        }
    }

    private void UpdateScriptHostRuntimeConfig()
    {
        // Modify runtimeconfig.json so script-host runs on .NET version we want.
        // The same .runtimeconfig.json file is used, but it is updated everytime before
        // starting a new script-host process.

        var runtimeConfigFilePath = Path.Combine(
            Path.GetDirectoryName(scriptHostExecutablePath.Path) ?? string.Empty,
            Path.GetFileNameWithoutExtension(scriptHostExecutablePath.Path) + ".runtimeconfig.json"
        );

        if (!File.Exists(runtimeConfigFilePath))
        {
            throw new FileNotFoundException(
                $"script-host runtimeconfig.json could not be found at: {runtimeConfigFilePath}");
        }

        int majorVersion = script.Config.TargetFrameworkVersion.GetMajorVersion();

        var root = JsonNode.Parse(File.ReadAllText(runtimeConfigFilePath))
                   ?? throw new Exception($"Could not deserialize runtimeconfig.json from {runtimeConfigFilePath}");

        var runtimeOptions = root["runtimeOptions"];
        if (runtimeOptions == null)
        {
            return;
        }

        if (runtimeOptions["tfm"] != null)
        {
            runtimeOptions["tfm"] = $"net{majorVersion}.0";
        }

        var frameworks = runtimeOptions["frameworks"];
        if (frameworks != null)
        {
            foreach (var framework in frameworks.AsArray())
            {
                if (framework != null)
                {
                    framework["version"] = $"{majorVersion}.0.0";
                }
            }
        }

        File.WriteAllText(
            runtimeConfigFilePath,
            root.ToJsonString()
        );
    }

    private void Cleanup()
    {
        while (_sendQueue.Reader.TryRead(out _))
        {
            // Discard items
        }

        if (_ipcGateway != null)
        {
            _ipcGateway.Dispose();
            _ipcGateway = null;
        }

        if (_scriptHostProcess != null)
        {
            try
            {
                _scriptHostProcess.KillIfRunning();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error killing script-host process");
            }

            _scriptHostProcess.Dispose();
            _scriptHostProcess = null;

            try
            {
                Retry.Execute(2, TimeSpan.FromSeconds(1), () => { Directory.Delete(scriptHostRootDir.Path, true); });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting script-host root dir: {Path}", scriptHostExecutablePath.Path);
            }
        }
    }
}

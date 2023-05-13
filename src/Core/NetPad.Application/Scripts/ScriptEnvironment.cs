using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.IO;
using NetPad.Runtimes;
using NetPad.Utilities;

namespace NetPad.Scripts;

// If this class is sealed, IDisposable and IAsyncDisposable implementations should be revised
public class ScriptEnvironment : IDisposable, IAsyncDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ScriptEnvironment> _logger;
    private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
    private readonly IServiceScope _serviceScope;
    private IInputReader<string> _inputReader;
    private IScriptOutputAdapter<ScriptOutput, ScriptOutput> _outputAdapter;
    private ScriptStatus _status;
    private IScriptRuntime<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>? _runtime;
    private bool _isDisposed;

    public ScriptEnvironment(Script script, IServiceScope serviceScope)
    {
        Script = script;
        _serviceScope = serviceScope;
        _eventBus = _serviceScope.ServiceProvider.GetRequiredService<IEventBus>();
        _dataConnectionResourcesCache = _serviceScope.ServiceProvider.GetRequiredService<IDataConnectionResourcesCache>();
        _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ScriptEnvironment>>();
        _inputReader = ActionInputReader<string>.Null;
        _outputAdapter = IScriptOutputAdapter<ScriptOutput, ScriptOutput>.Null;
        _status = ScriptStatus.Ready;

        Initialize();
    }

    public Script Script { get; }

    public virtual ScriptStatus Status => _status;

    public double RunDurationMilliseconds { get; private set; }

    public async Task RunAsync(RunOptions runOptions)
    {
        EnsureNotDisposed();

        _logger.LogTrace($"{nameof(RunAsync)} start");

        if (Status == ScriptStatus.Running)
            throw new InvalidOperationException("Script is already running.");

        await SetStatusAsync(ScriptStatus.Running);

        try
        {
            if (Script.DataConnection != null)
            {
                await AppendDataConnectionResourcesAsync(runOptions, Script.DataConnection);
            }

            // Script could have been requested to stop by this point
            if (Status == ScriptStatus.Stopping) return;

            var runtime = await GetRuntimeAsync();

            if (Status == ScriptStatus.Stopping) return;

            var runResult = await runtime.RunScriptAsync(runOptions);

            await SetRunDurationAsync(runResult.DurationMs);
            await SetStatusAsync(runResult.IsScriptCompletedSuccessfully ? ScriptStatus.Ready : ScriptStatus.Error);

            _logger.LogDebug("Run completed with status: {Status}",
                runResult.IsScriptCompletedSuccessfully
                    ? "Success"
                    : runResult.IsRunAttemptSuccessful
                        ? "Failure in script code"
                        : "Could not run");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(ex));
            await SetStatusAsync(ScriptStatus.Error);
        }
        finally
        {
            _logger.LogTrace($"{nameof(RunAsync)} end");
        }
    }

    private async Task AppendDataConnectionResourcesAsync(RunOptions runOptions, DataConnection dataConnection)
    {
        var connectionCode = await _dataConnectionResourcesCache.GetSourceGeneratedCodeAsync(dataConnection);
        if (connectionCode.ApplicationCode.Any())
        {
            runOptions.AdditionalCode.AddRange(connectionCode.ApplicationCode);
        }

        if (dataConnection.Type == DataConnectionType.MSSQLServer)
        {
            // Special case for MS SQL Server. When targeting a MS SQL server database, we must load the
            // os-specific version of Microsoft.Data.SqlClient.dll that MSBuild copies for us in
            // a specific dir (in app .csproj file). This behavior is an issue where nuget does not
            // resolve the correct os-specific version of the assembly
            // See:
            // https://github.com/dotnet/SqlClient/issues/1631#issuecomment-1280103212
            var appExePath = Assembly.GetEntryAssembly()?.Location;
            if (appExePath != null && File.Exists(appExePath))
            {
                FilePath sqlClientAssemblyToCopy;
                FilePath subDirSqlClientAssemblyOverride = Path.Combine(
                    Path.GetDirectoryName(appExePath)!,
                    "Microsoft.Data.SqlClient",
                    "Microsoft.Data.SqlClient.dll");

                if (subDirSqlClientAssemblyOverride.Exists())
                {
                    // Used for DEBUG builds for both Unix and Windows
                    // When building in DEBUG, MS Build will not copy the correct os-specific assembly to
                    // the app's directory. So MS Build is configured to copy the correct version of the
                    // assembly into a Microsoft.Data.SqlClient sub-directory (in app's .csproj file),
                    // and here we need to copy this version of the assembly and overwrite the one that
                    // is resolved using nuget when the script is ran..
                    sqlClientAssemblyToCopy = subDirSqlClientAssemblyOverride;

                    // Used for Windows DEBUG builds. The Microsoft.Data.SqlClient.dll assembly that was
                    // copied to the Microsoft.Data.SqlClient sub-directory in the statement above
                    // requires it to run. MS Build is configured, like the assembly above, to copy the
                    // os-specific version of the assembly to the output directory (in app's .csproj file).
                    if (PlatformUtil.IsWindowsPlatform())
                    {
                        runOptions.Assets.Add(new RunAsset(
                            Path.Combine(
                                Path.GetDirectoryName(appExePath)!,
                                "Microsoft.Data.SqlClient",
                                "Microsoft.Data.SqlClient.SNI.dll"),
                            "./Microsoft.Data.SqlClient.SNI.dll"));
                    }
                }
                else
                {
                    // Used for RELEASE builds for both Unix and Windows.
                    // When building the release version MS Build detects the correct os-specific version
                    // of the assembly and packages it with the app. However when nuget is used to get
                    // the Microsoft.Data.SqlClient.dll assembly file needed to run the script
                    // (as defined by the MS SQL Server data connection dependencies) it resolves an
                    // assembly that throws a PlatformNotSupported exception systems.
                    // Here we need to copy the assembly that was shipped with the RELEASE
                    // build of the app
                    sqlClientAssemblyToCopy = Path.Combine(
                        Path.GetDirectoryName(appExePath)!,
                        "Microsoft.Data.SqlClient.dll");
                }

                runOptions.Assets.Add(new RunAsset(sqlClientAssemblyToCopy, "./Microsoft.Data.SqlClient.dll"));
            }
        }

        var connectionAssembly = await _dataConnectionResourcesCache.GetAssemblyAsync(dataConnection);
        if (connectionAssembly != null)
        {
            runOptions.AdditionalReferences.Add(new AssemblyImageReference(connectionAssembly));
        }

        var requiredReferences = await _dataConnectionResourcesCache.GetRequiredReferencesAsync(dataConnection);

        if (requiredReferences.Any())
        {
            runOptions.AdditionalReferences.AddRange(requiredReferences);
        }
    }

    public async Task StopAsync()
    {
        EnsureNotDisposed();

        _logger.LogTrace($"{nameof(StopAsync)} start");

        if (Status != ScriptStatus.Running)
            throw new InvalidOperationException("Script is not running.");

        await SetStatusAsync(ScriptStatus.Stopping);

        try
        {
            // The runtime might not have been initialized yet which means no running is taking place
            if (_runtime != null)
            {
                await _runtime.StopScriptAsync();
            }

            await _outputAdapter.ResultsChannel.WriteAsync(
                new RawScriptOutput($"\n# Script stopped on: {DateTime.Now}"));
            await SetStatusAsync(ScriptStatus.Ready);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping script");
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(ex));
            await SetStatusAsync(ScriptStatus.Error);
        }
        finally
        {
            _logger.LogTrace($"{nameof(StopAsync)} end");
        }
    }

    public void SetIO(IInputReader<string> inputReader, IScriptOutputAdapter<ScriptOutput, ScriptOutput> outputAdapter)
    {
        EnsureNotDisposed();

        RemoveScriptRuntimeIOHandlers();

        _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        _outputAdapter = outputAdapter ?? throw new ArgumentNullException(nameof(outputAdapter));

        AddScriptRuntimeIOHandlers();
    }

    private void Initialize()
    {
        Script.OnPropertyChanged.Add(async args =>
        {
            await _eventBus.PublishAsync(new ScriptPropertyChangedEvent(Script.Id, args.PropertyName,
                args.NewValue));
        });

        Script.Config.OnPropertyChanged.Add(async args =>
        {
            await _eventBus.PublishAsync(
                new ScriptConfigPropertyChangedEvent(Script.Id, args.PropertyName, args.NewValue));
        });
    }

    private async Task SetStatusAsync(ScriptStatus status)
    {
        _status = status;
        await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(Status), status));
    }

    private async Task SetRunDurationAsync(double runDurationMs)
    {
        RunDurationMilliseconds = runDurationMs;
        await _eventBus.PublishAsync(
            new EnvironmentPropertyChangedEvent(Script.Id, nameof(RunDurationMilliseconds), runDurationMs));
    }

    private async Task<IScriptRuntime> GetRuntimeAsync()
    {
        if (_runtime == null)
        {
            _logger.LogDebug("Initializing new runtime");

            var factory = _serviceScope.ServiceProvider.GetRequiredService<IScriptRuntimeFactory>();
            _runtime = await factory.CreateScriptRuntimeAsync(Script);

            AddScriptRuntimeIOHandlers();
        }

        return _runtime;
    }

    private void AddScriptRuntimeIOHandlers()
    {
        _runtime?.AddInput(_inputReader);
        _runtime?.AddOutput(_outputAdapter);
    }

    private void RemoveScriptRuntimeIOHandlers()
    {
        _runtime?.RemoveInput(_inputReader);
        _runtime?.RemoveOutput(_outputAdapter);
    }

    private void EnsureNotDisposed()
    {
        if (_isDisposed)
            throw new InvalidOperationException($"Script environment {Script.Id} is disposed.");
    }

    public void Dispose()
    {
        _logger.LogTrace($"{nameof(Dispose)} start");

        Dispose(true);
        GC.SuppressFinalize(this);

        _isDisposed = true;

        _logger.LogTrace($"{nameof(Dispose)} end");
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogTrace($"{nameof(DisposeAsync)} start");

        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

        _isDisposed = true;

        _logger.LogTrace($"{nameof(DisposeAsync)} end");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _runtime?.Dispose();
            _runtime = null;

            _serviceScope.Dispose(); // won't this dispose the runtime anyways?

            Script.RemoveAllPropertyChangedHandlers();
            Script.Config.RemoveAllPropertyChangedHandlers();
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_runtime != null)
        {
            await _runtime.DisposeAsync().ConfigureAwait(false);
        }

        if (_serviceScope is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _serviceScope.Dispose();
        }

        _runtime = null;
    }
}

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Application;
using NetPad.Compilation;
using NetPad.Compilation.Scripts;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Data.Security;
using NetPad.DotNet;
using NetPad.DotNet.CodeAnalysis;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.ClientServer;
using NetPad.ExecutionModel.ClientServer.ScriptHost;
using NetPad.IO;
using NetPad.IO.IPC.Stdio;
using NetPad.Presentation;
using NetPad.Scripts;

namespace NetPad.Runtime.Tests.ExecutionModel.ClientServer;

public class ClientServerScriptRunnerTests : IDisposable
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    private readonly Script _script;
    private readonly Mock<IScriptDependencyResolver> _depResolver;
    private readonly Mock<IScriptCompiler> _compiler;
    private readonly Mock<IAppStatusMessagePublisher> _statusPublisher;
    private readonly Mock<IDotNetInfo> _dotNetInfo;
    private readonly EventBus _eventBus;
    private readonly Mock<IScriptHostProcessManager> _processManager;
    private readonly Mock<IScriptHostProcessManagerFactory> _processManagerFactory;
    private readonly CapturingOutputWriter _outputCapture;
    private readonly Settings _settings;

    // Signal that fires when the mock process manager's RunScript is called
    private TaskCompletionSource _runScriptCalled = new();

    public ClientServerScriptRunnerTests()
    {
        _script = ScriptTestHelper.CreateScript();
        _script.UpdateCode("Console.WriteLine(1);");

        _depResolver = new Mock<IScriptDependencyResolver>();
        _depResolver
            .Setup(r => r.GetDependenciesAsync(It.IsAny<Script>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScriptDependencies([], []));

        _compiler = new Mock<IScriptCompiler>();

        _statusPublisher = new Mock<IAppStatusMessagePublisher>();
        _statusPublisher
            .Setup(p => p.PublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AppStatusMessagePriority>(),
                It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        _dotNetInfo = new Mock<IDotNetInfo>();

        _eventBus = new EventBus();
        _settings = new Settings();

        _processManager = new Mock<IScriptHostProcessManager>();
        _processManager
            .Setup(pm => pm.RunScript(
                It.IsAny<Guid>(),
                It.IsAny<DirectoryPath>(),
                It.IsAny<DirectoryPath>(),
                It.IsAny<FilePath>(),
                It.IsAny<string[]>()))
            .Callback(() => _runScriptCalled.TrySetResult());

        _processManagerFactory = new Mock<IScriptHostProcessManagerFactory>();
        _processManagerFactory
            .Setup(f => f.Create(
                It.IsAny<Script>(),
                It.IsAny<WorkingDirectory>(),
                It.IsAny<Action<StdioIpcGateway>>(),
                It.IsAny<Action<string>>(),
                It.IsAny<Action<string>>()))
            .Returns(_processManager.Object);

        _outputCapture = new CapturingOutputWriter();
    }

    private TestableScriptRunner CreateRunner(Script? script = null)
    {
        var runner = new TestableScriptRunner(
            script ?? _script,
            _depResolver.Object,
            _compiler.Object,
            _statusPublisher.Object,
            _dotNetInfo.Object,
            _eventBus,
            _settings,
            NullLogger<ClientServerScriptRunner>.Instance,
            _processManagerFactory.Object);

        runner.AddOutput(_outputCapture);
        return runner;
    }

    /// <summary>
    /// Waits for the background Task.Run to reach the point where RunScript is called,
    /// then resets the signal for the next run.
    /// </summary>
    private async Task WaitForRunScriptCalled()
    {
        await _runScriptCalled.Task.WaitAsync(TestTimeout);
        _runScriptCalled = new TaskCompletionSource();
    }

    /// <summary>
    /// Starts a run, waits for it to reach the process manager, then stops it.
    /// Returns after the run is fully stopped.
    /// </summary>
    private async Task RunAndStop(ClientServerScriptRunner runner)
    {
        _ = runner.RunScriptAsync(new RunOptions());
        await WaitForRunScriptCalled();
        await runner.StopScriptAsync();
    }

    private void SetupCompilationSuccess()
    {
        var parsingResult = new CodeParsingResult(
            new SourceCode(new Code("user code")),
            new SourceCode(new Code("bootstrapper")),
            null);

        var compilationResult = new CompilationResult(
            true,
            new AssemblyName("TestScript"),
            "TestScript.dll",
            [0x00],
            ImmutableArray<Diagnostic>.Empty);

        _compiler
            .Setup(c => c.ParseAndCompileAsync(It.IsAny<string>(), It.IsAny<Script>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParseAndCompileResult(parsingResult, compilationResult));
    }

    private void SetupCompilationFailure()
    {
        var parsingResult = new CodeParsingResult(
            new SourceCode(new Code("bad code")),
            new SourceCode(new Code("bootstrapper")),
            null);

        var compilationResult = new CompilationResult(
            false,
            new AssemblyName("TestScript"),
            "TestScript.dll",
            [],
            ImmutableArray<Diagnostic>.Empty);

        _compiler
            .Setup(c => c.ParseAndCompileAsync(It.IsAny<string>(), It.IsAny<Script>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParseAndCompileResult(parsingResult, compilationResult));
    }

    public void Dispose()
    {
    }

    #region Compilation Failure

    [Fact]
    public async Task CompilationFailure_ReturnsRunAttemptFailure()
    {
        SetupCompilationFailure();
        using var runner = CreateRunner();

        var result = await runner.RunScriptAsync(new RunOptions());

        Assert.False(result.IsRunAttemptSuccessful);
    }

    [Fact]
    public async Task CompilationFailure_NeverCallsProcessManager()
    {
        SetupCompilationFailure();
        using var runner = CreateRunner();

        await runner.RunScriptAsync(new RunOptions());

        _processManager.Verify(
            pm => pm.RunScript(
                It.IsAny<Guid>(),
                It.IsAny<DirectoryPath>(),
                It.IsAny<DirectoryPath>(),
                It.IsAny<FilePath>(),
                It.IsAny<string[]>()),
            Times.Never);
    }

    [Fact]
    public async Task CompilationFailure_WritesErrorOutput()
    {
        SetupCompilationFailure();
        using var runner = CreateRunner();

        await runner.RunScriptAsync(new RunOptions());

        Assert.Contains(_outputCapture.Writes, o => o is ErrorScriptOutput);
    }

    #endregion

    #region Successful Run

    [Fact]
    public async Task SuccessfulCompilation_CallsProcessManagerRunScript()
    {
        SetupCompilationSuccess();
        using var runner = CreateRunner();

        _ = runner.RunScriptAsync(new RunOptions());
        await WaitForRunScriptCalled();

        _processManager.Verify(
            pm => pm.RunScript(
                It.IsAny<Guid>(),
                It.IsAny<DirectoryPath>(),
                It.IsAny<DirectoryPath>(),
                It.IsAny<FilePath>(),
                It.IsAny<string[]>()),
            Times.Once);

        await runner.StopScriptAsync();
    }

    #endregion

    #region Already Running Guard

    [Fact]
    public async Task RunWhileAlreadyRunning_ReturnsSameTask()
    {
        SetupCompilationSuccess();
        using var runner = CreateRunner();

        var task1 = runner.RunScriptAsync(new RunOptions());
        await WaitForRunScriptCalled();

        var task2 = runner.RunScriptAsync(new RunOptions());

        Assert.Same(task1, task2);

        await runner.StopScriptAsync();
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task StopScript_ReturnsRunCancelled()
    {
        SetupCompilationSuccess();
        using var runner = CreateRunner();

        var runTask = runner.RunScriptAsync(new RunOptions());
        await WaitForRunScriptCalled();

        await runner.StopScriptAsync();
        var result = await runTask;

        Assert.True(result.IsRunCancelled);
    }

    #endregion

    #region Script-Host Restart

    [Fact]
    public async Task AfterUserStop_NextRunRestartsScriptHost()
    {
        SetupCompilationSuccess();
        using var runner = CreateRunner();

        // First run, then stop
        await RunAndStop(runner);

        // Reset mock to track calls from second run only
        _processManager.Invocations.Clear();

        // Second run — should restart because user requested stop
        _ = runner.RunScriptAsync(new RunOptions());
        await WaitForRunScriptCalled();

        _processManager.Verify(pm => pm.StopScriptHost(), Times.Once);

        await runner.StopScriptAsync();
    }

    [Fact]
    public void HasScriptChangedEnoughToRestartScriptHost_DetectsFrameworkChange()
    {
        var script = ScriptTestHelper.CreateScript(frameworkVersion: DotNetFrameworkVersion.DotNet9);
        var run = new ScriptRun(script);

        script.Config.SetTargetFrameworkVersion(DotNetFrameworkVersion.DotNet8);

        Assert.True(run.HasScriptChangedEnoughToRestartScriptHost(script));
        run.Dispose();
    }

    [Fact]
    public void HasScriptChangedEnoughToRestartScriptHost_DetectsDataConnectionChange()
    {
        var script = ScriptTestHelper.CreateScript();
        var dataConnection = CreateMockDataConnection();
        script.SetDataConnection(dataConnection);
        var run = new ScriptRun(script);

        script.SetDataConnection(null);

        Assert.True(run.HasScriptChangedEnoughToRestartScriptHost(script));
        run.Dispose();
    }

    #endregion

    #region Cache Invalidation

    [Fact]
    public async Task DataConnectionResourcesUpdating_InvalidatesCacheAndRecompilesOnNextRun()
    {
        SetupCompilationSuccess();
        var dataConnection = CreateMockDataConnection();
        _script.SetDataConnection(dataConnection);
        using var runner = CreateRunner();

        // First run, then stop
        await RunAndStop(runner);

        // Publish event — simulates data connection resources changing
        await _eventBus.PublishAsync(
            new DataConnectionResourcesUpdatingEvent(dataConnection, DotNetFrameworkVersion.DotNet9));

        _compiler.Invocations.Clear();

        // Second run — should recompile (cache was invalidated)
        _ = runner.RunScriptAsync(new RunOptions());
        await WaitForRunScriptCalled();

        _compiler.Verify(
            c => c.ParseAndCompileAsync(It.IsAny<string>(), It.IsAny<Script>(), It.IsAny<CancellationToken>()),
            Times.Once);

        await runner.StopScriptAsync();
    }

    [Fact]
    public async Task CompilationCacheHit_SkipsRecompilation()
    {
        SetupCompilationSuccess();
        using var runner = CreateRunner();

        // First run — compiles
        await RunAndStop(runner);

        _compiler.Invocations.Clear();

        // Second run with same code — should use cache
        _ = runner.RunScriptAsync(new RunOptions());
        await WaitForRunScriptCalled();

        _compiler.Verify(
            c => c.ParseAndCompileAsync(It.IsAny<string>(), It.IsAny<Script>(), It.IsAny<CancellationToken>()),
            Times.Never);

        await runner.StopScriptAsync();
    }

    #endregion

    #region CorrectUncaughtExceptionStackTraceLineNumber

    [Fact]
    public void LineCorrection_AdjustsLineNumbers()
    {
        var input = "Unhandled exception. System.Exception: error\n   at Program.Main() :line 15";

        var result = ClientServerScriptRunner.CorrectUncaughtExceptionStackTraceLineNumber(input, 10);

        Assert.Contains(":line5", result);
        Assert.DoesNotContain(":line 15", result);
    }

    [Fact]
    public void LineCorrection_MultipleLines_CorrectsBoth()
    {
        var input = "   at Foo() :line 20\n   at Bar() :line 30";

        var result = ClientServerScriptRunner.CorrectUncaughtExceptionStackTraceLineNumber(input, 5);

        Assert.Contains(":line15", result);
        Assert.Contains(":line25", result);
    }

    #endregion

    #region Helpers

    private static DataConnection CreateMockDataConnection()
    {
        var mock = new Mock<DataConnection>(Guid.NewGuid(), "TestDb", DataConnectionType.PostgreSQL)
        {
            CallBase = true
        };
        mock.Setup(x => x.TestConnectionAsync(It.IsAny<IDataConnectionPasswordProtector>()))
            .ReturnsAsync(new DataConnectionTestResult(true));
        return mock.Object;
    }

    /// <summary>
    /// Subclass that overrides filesystem deployment methods to no-op.
    /// </summary>
    private class TestableScriptRunner(
        Script script,
        IScriptDependencyResolver scriptDependencyResolver,
        IScriptCompiler scriptCompiler,
        IAppStatusMessagePublisher appStatusMessagePublisher,
        IDotNetInfo dotNetInfo,
        IEventBus eventBus,
        Settings settings,
        ILogger<ClientServerScriptRunner> logger,
        IScriptHostProcessManagerFactory scriptHostProcessManagerFactory)
        : ClientServerScriptRunner(
            script, scriptDependencyResolver, scriptCompiler, appStatusMessagePublisher,
            dotNetInfo, eventBus, settings, logger, scriptHostProcessManagerFactory)
    {
        protected override void DeployScriptHostExecutable(WorkingDirectory workingDirectory) { }

        protected override Task DeploySharedDependenciesAsync(
            WorkingDirectory workingDirectory, ScriptDependencies dependencies) => Task.CompletedTask;

        protected override Task<(DirectoryPath, FilePath)> DeployScriptDependenciesAsync(
            byte[] scriptAssembly, ScriptDependencies dependencies)
            => Task.FromResult<(DirectoryPath, FilePath)>((
                new DirectoryPath(Path.GetTempPath()),
                new FilePath(Path.Combine(Path.GetTempPath(), "test-script.dll"))));

        protected override Task WriteScriptConfigAsync(DirectoryPath scriptDeployDir) => Task.CompletedTask;
    }

    private class CapturingOutputWriter : IOutputWriter<object>
    {
        public ConcurrentBag<object> Writes { get; } = [];

        public Task WriteAsync(object? output, string? title = null, CancellationToken cancellationToken = default)
        {
            if (output != null) Writes.Add(output);
            return Task.CompletedTask;
        }
    }

    #endregion
}

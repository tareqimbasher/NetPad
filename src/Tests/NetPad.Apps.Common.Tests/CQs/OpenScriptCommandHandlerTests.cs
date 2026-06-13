using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Application;
using NetPad.Apps.Common.Tests.Helpers;
using NetPad.Apps.CQs;
using NetPad.Apps.Services;
using NetPad.Apps.UiInterop;
using NetPad.Common;
using NetPad.Data;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions;

namespace NetPad.Apps.Common.Tests.CQs;

public class OpenScriptCommandHandlerTests
{
    private readonly Mock<IScriptRepository> _scriptRepository = new();
    private readonly EventBus _eventBus = new();
    private readonly Mock<IUiDialogService> _uiDialogService = new();
    private readonly Mock<IRecentScriptsService> _recentScriptsService = new();
    private readonly Mock<IAppStatusMessagePublisher> _appStatusMessagePublisher = new();
    private readonly Session _session;
    private readonly OpenScriptCommand.Handler _handler;
    private readonly List<ScriptOpenedEvent> _scriptOpenedEvents = [];

    public OpenScriptCommandHandlerTests()
    {
        // Real Session over a minimal DI container so ScriptEnvironment construction works.
        var services = new ServiceCollection();
        services.AddSingleton<IEventBus>(_eventBus);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ITrivialDataStore>(new InMemoryTrivialDataStore());
        // ScriptEnvironment's ctor requests a runner. These tests never run scripts, so a no-op
        // factory that returns a stub runner is enough to satisfy DI.
        services.AddSingleton<IScriptRunnerFactory>(new NoopScriptRunnerFactory());
        var provider = services.BuildServiceProvider();

        _session = new Session(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ITrivialDataStore>(),
            _eventBus,
            NullLogger<Session>.Instance);

        _eventBus.Subscribe<ScriptOpenedEvent>(ev =>
        {
            lock (_scriptOpenedEvents)
            {
                _scriptOpenedEvents.Add(ev);
            }
            return Task.CompletedTask;
        }, useStrongReferences: true);

        _handler = new OpenScriptCommand.Handler(
            _scriptRepository.Object,
            _session,
            _eventBus,
            _uiDialogService.Object,
            _recentScriptsService.Object,
            _appStatusMessagePublisher.Object);
    }

    [Fact]
    public async Task Throws_When_Path_Is_Whitespace_And_No_Script_Or_Id()
    {
        // Whitespace path is the only way to reach the "not enough information" branch through
        // the public constructors: every ctor sets exactly one of Script/Id/Path.
        var cmd = new OpenScriptCommand("   ");

        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Open_By_Script_Opens_Script_And_Publishes_Event_Without_Touching_Recents()
    {
        var script = CreateScript();
        var cmd = new OpenScriptCommand(script);

        var env = await _handler.Handle(cmd, CancellationToken.None);

        Assert.Same(script, env.Script);
        Assert.Single(_scriptOpenedEvents);
        // No path on a brand-new script, so recents shouldn't be touched.
        _recentScriptsService.Verify(r => r.Add(It.IsAny<string>()), Times.Never);
        // Open-by-Script is not openedFromPath, so the dup dialog must never be reached.
        _uiDialogService.Verify(
            d => d.AskUserToOpenAsDuplicate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Open_By_Id_Loads_From_Repository_And_Opens()
    {
        var script = CreateScript();
        script.SetPath("/scripts/hello.netpad");
        _scriptRepository.Setup(r => r.GetAsync(script.Id)).ReturnsAsync(script);

        var env = await _handler.Handle(new OpenScriptCommand(script.Id), CancellationToken.None);

        Assert.Same(script, env.Script);
        Assert.Single(_scriptOpenedEvents);
        _recentScriptsService.Verify(r => r.Add(script.Path!), Times.Once);
        _uiDialogService.Verify(
            d => d.AskUserToOpenAsDuplicate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Open_By_Path_With_No_Existing_Opens_And_Updates_Recents()
    {
        const string path = "/scripts/hello.netpad";
        var script = CreateScript();
        script.SetPath(path);
        _scriptRepository.Setup(r => r.GetAsync(path)).ReturnsAsync(script);

        var env = await _handler.Handle(new OpenScriptCommand(path), CancellationToken.None);

        Assert.Same(script, env.Script);
        Assert.Single(_scriptOpenedEvents);
        _recentScriptsService.Verify(r => r.Add(script.Path!), Times.Once);
        _uiDialogService.Verify(
            d => d.AskUserToOpenAsDuplicate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Open_By_Path_With_Existing_At_Same_Path_Falls_Through_And_Dedupes()
    {
        const string path = "/scripts/hello.netpad";

        var existingScript = CreateScript();
        existingScript.SetPath(path);
        var existingEnv = await _session.OpenAsync(existingScript, false);

        // Repo returns a NEW Script instance with the same Id (as if reloaded from disk).
        var reloaded = CreateScript(existingScript.Id);
        reloaded.SetPath(path);
        _scriptRepository.Setup(r => r.GetAsync(path)).ReturnsAsync(reloaded);

        var env = await _handler.Handle(new OpenScriptCommand(path), CancellationToken.None);

        // Session dedupes by Id — same environment returned.
        Assert.Same(existingEnv, env);
        Assert.Single(_session.GetOpened());
        // No duplicate prompt for matching paths.
        _uiDialogService.Verify(
            d => d.AskUserToOpenAsDuplicate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        // Falls through to the bottom of Handle, so recents IS bumped and event IS published.
        _recentScriptsService.Verify(r => r.Add(path), Times.Once);
        Assert.Single(_scriptOpenedEvents);
    }

    [Fact]
    public async Task Open_By_Path_When_Existing_Has_Null_Path_Rebinds_Without_Replacing_Content()
    {
        const string newPath = "/scripts/found-again.netpad";

        // Existing environment has no path (orphan recovery scenario).
        var existingScript = CreateScript();
        existingScript.UpdateCode("recovered content");
        Assert.Null(existingScript.Path);
        var existingEnv = await _session.OpenAsync(existingScript, false);

        // The on-disk version we're opening has the same Id but different content.
        var onDisk = CreateScript(existingScript.Id);
        onDisk.UpdateCode("on-disk content");
        onDisk.SetPath(newPath);
        _scriptRepository.Setup(r => r.GetAsync(newPath)).ReturnsAsync(onDisk);

        var env = await _handler.Handle(new OpenScriptCommand(newPath), CancellationToken.None);

        // Same environment returned (rebound, not replaced).
        Assert.Same(existingEnv, env);
        Assert.Same(existingScript, env.Script);
        // Path is now bound to the file the user just opened.
        Assert.Equal(newPath, env.Script.Path);
        // Existing in-memory code preserved — we did NOT swap in the on-disk content.
        Assert.Equal("recovered content", env.Script.Code);
        // Still only one environment.
        Assert.Single(_session.GetOpened());
        // Recents updated; ScriptOpenedEvent NOT published (rebind branch returns early).
        _recentScriptsService.Verify(r => r.Add(newPath), Times.Once);
        Assert.Empty(_scriptOpenedEvents);
        // Recovered content differs from the file on disk, so a non-blocking warning is published —
        // the substitution is surfaced rather than silent.
        _appStatusMessagePublisher.Verify(
            p => p.PublishAsync(existingScript.Id, It.IsAny<string>(), AppStatusMessagePriority.High, It.IsAny<bool>()),
            Times.Once);
        // No duplicate prompt — the rebind path takes precedence.
        _uiDialogService.Verify(
            d => d.AskUserToOpenAsDuplicate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Open_By_Path_When_Existing_Has_Null_Path_And_Content_Matches_Rebinds_Without_Warning()
    {
        const string newPath = "/scripts/found-again.netpad";

        // Orphan-recovered environment with no path, whose content matches what's on disk.
        var existingScript = CreateScript();
        existingScript.UpdateCode("same content");
        Assert.Null(existingScript.Path);
        var existingEnv = await _session.OpenAsync(existingScript, false);

        // On-disk version: same Id, identical content (so the fingerprints match).
        var onDisk = CreateScript(existingScript.Id);
        onDisk.UpdateCode("same content");
        onDisk.SetPath(newPath);
        _scriptRepository.Setup(r => r.GetAsync(newPath)).ReturnsAsync(onDisk);

        var env = await _handler.Handle(new OpenScriptCommand(newPath), CancellationToken.None);

        // Rebound to the file, content preserved, single environment — same as the differing case.
        Assert.Same(existingEnv, env);
        Assert.Equal(newPath, env.Script.Path);
        Assert.Single(_session.GetOpened());
        _recentScriptsService.Verify(r => r.Add(newPath), Times.Once);
        Assert.Empty(_scriptOpenedEvents);
        // Content matches disk, so there's nothing to warn about — no status message published.
        _appStatusMessagePublisher.Verify(
            p => p.PublishAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AppStatusMessagePriority>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Open_By_Path_With_Different_Path_And_User_Confirms_Clones_With_New_Id()
    {
        const string existingPath = "/scripts/original.netpad";
        const string newPath = "/scripts/copy.netpad";

        var existingScript = CreateScript();
        existingScript.SetPath(existingPath);
        var existingEnv = await _session.OpenAsync(existingScript, false);

        // The new file shares the existing Id (e.g. an OS-level copy of the file).
        var onDisk = CreateScript(existingScript.Id, name: "copy");
        onDisk.SetPath(newPath);
        _scriptRepository.Setup(r => r.GetAsync(newPath)).ReturnsAsync(onDisk);

        _uiDialogService
            .Setup(d => d.AskUserToOpenAsDuplicate(newPath, existingPath))
            .ReturnsAsync(true);

        var env = await _handler.Handle(new OpenScriptCommand(newPath), CancellationToken.None);

        // A new environment was created from a clone with a fresh Id.
        Assert.NotSame(existingEnv, env);
        Assert.NotEqual(existingScript.Id, env.Script.Id);
        Assert.Equal(newPath, env.Script.Path);
        Assert.Equal(2, _session.GetOpened().Count);
        // Clone must be marked dirty: the on-disk file at newPath still holds the OLD Id,
        // so the in-memory state genuinely differs from disk. Without IsDirty=true the
        // close-prompt wouldn't fire and the duplicate-Id resolution would evaporate on close.
        Assert.True(env.Script.IsDirty);
        // The on-disk Script instance was NOT used directly — its Id is the existing Id, but the
        // environment's Script is a clone with a fresh Id.
        Assert.NotSame(onDisk, env.Script);
        // Existing environment's path untouched.
        Assert.Equal(existingPath, existingEnv.Script.Path);
        // ScriptOpenedEvent published for the cloned script; recents bumped.
        Assert.Single(_scriptOpenedEvents);
        Assert.Same(env.Script, _scriptOpenedEvents[0].Script);
        _recentScriptsService.Verify(r => r.Add(newPath), Times.Once);

        _uiDialogService.Verify(
            d => d.AskUserToOpenAsDuplicate(newPath, existingPath), Times.Once);
    }

    [Fact]
    public async Task Open_By_Path_With_Different_Path_And_User_Declines_Activates_Existing()
    {
        const string existingPath = "/scripts/original.netpad";
        const string newPath = "/scripts/copy.netpad";

        var existingScript = CreateScript();
        existingScript.SetPath(existingPath);
        var existingEnv = await _session.OpenAsync(existingScript, false);

        var onDisk = CreateScript(existingScript.Id);
        onDisk.SetPath(newPath);
        _scriptRepository.Setup(r => r.GetAsync(newPath)).ReturnsAsync(onDisk);

        _uiDialogService
            .Setup(d => d.AskUserToOpenAsDuplicate(newPath, existingPath))
            .ReturnsAsync(false);

        var env = await _handler.Handle(new OpenScriptCommand(newPath), CancellationToken.None);

        // Existing environment returned, no new environment created.
        Assert.Same(existingEnv, env);
        Assert.Single(_session.GetOpened());
        // Existing path unchanged.
        Assert.Equal(existingPath, existingEnv.Script.Path);
        // Decline branch returns early — no event, no recents add for this attempt.
        Assert.Empty(_scriptOpenedEvents);
        _recentScriptsService.Verify(r => r.Add(It.IsAny<string>()), Times.Never);
    }

    private static Script CreateScript(Guid? id = null, string? name = null)
    {
        id ??= Guid.NewGuid();
        name ??= $"Script {id}";
        return new Script(id.Value, name, new ScriptConfig(ScriptKind.Program, GlobalConsts.AppDotNetFrameworkVersion));
    }

    private class NoopScriptRunnerFactory : IScriptRunnerFactory
    {
        public IScriptRunner CreateRunner(Script script) => new Mock<IScriptRunner>().Object;
    }
}

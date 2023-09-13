using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Data;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using NetPad.Tests.Services;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Application.Tests.Sessions;

public class SessionTests : TestBase
{
    public SessionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddTransient<IDataConnectionResourcesCache, NullDataConnectionResourcesCache>();

        base.ConfigureServices(services);
    }

    [Fact]
    public async Task GettingOpenedScriptById_ReturnsCorrectScript()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script);

        var result = session.Get(script.Id);

        Assert.Equal(script, result?.Script);
    }

    [Fact]
    public async Task GettingClosedScriptById_ReturnsNull()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script);
        await session.CloseAsync(script.Id);

        var result = session.Get(script.Id);

        Assert.Null(result);
    }

    [Fact]
    public void GettingNonOpenedScriptById_ReturnsNull()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);

        var result = session.Get(Guid.NewGuid());

        Assert.Null(result);
    }


    [Fact]
    public void ActiveSession_IsNull_OnInitialization()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);

        Assert.Null(session.Active);
    }

    [Fact]
    public async Task ActivingAScript_SetsItAsTheActiveScript()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script1 = ScriptTestHelper.CreateScript();
        var script2 = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script1);
        await session.OpenAsync(script2);

        await session.ActivateAsync(script1.Id);

        Assert.Equal(session.Active?.Script, script1);
    }

    [Fact]
    public async Task ActivatingLastActiveScript_ActivatesTheLastActiveScript()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script1 = ScriptTestHelper.CreateScript();
        var script2 = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script1);
        await session.OpenAsync(script2);
        await session.ActivateAsync(script1.Id);

        await session.ActivateLastActiveScriptAsync();

        Assert.Equal(session.Active?.Script, script2);
    }

    [Fact]
    public async Task ActivatingLastActiveScriptWhenNoScriptsAreOpen_DoesNotThrow()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);

        await session.ActivateLastActiveScriptAsync();

        Assert.Null(session.Active);
    }

    [Fact]
    public async Task ActivatingLastActiveScriptWhenNoScriptWasLastActive_DoesNotChangeActiveProperty()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script);

        await session.ActivateLastActiveScriptAsync();

        Assert.Equal(session.Active?.Script, script);
    }

    [Fact]
    public async Task OpeningAScript_SetsItAsActive()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script);

        Assert.Equal(session.Active?.Script, script);
    }

    [Fact]
    public async Task OpeningAScript_AddsItToEnvironmentsCollection()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script);

        Assert.Equal(session.Environments.Single().Script, script);
    }

    [Fact]
    public async Task ClosingScript_RemovesItFromEnvironmentsCollection()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script);

        await session.CloseAsync(script.Id);

        Assert.Empty(session.Environments);
    }

    [Fact]
    public async Task ClosingActiveScript_WhenLastActiveScriptExists_MakesLastActiveScriptActive()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script1 = ScriptTestHelper.CreateScript();
        var script2 = ScriptTestHelper.CreateScript();
        var script3 = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script2);
        await session.OpenAsync(script1);
        await session.OpenAsync(script3);

        await session.CloseAsync(script3.Id);

        Assert.Equal(script1, session.Active?.Script);
    }

    [Fact]
    public async Task ClosingLastActiveScript_SetsActiveToNull()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script);

        await session.CloseAsync(script.Id);

        Assert.Null(session.Active);
    }

    [Fact]
    public async Task ClosingActiveScript_WhenLastActiveScriptWasAlsoClosed_ActivatesScriptBeforeClosingActiveScript()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script1 = ScriptTestHelper.CreateScript();
        var script2 = ScriptTestHelper.CreateScript();
        var script3 = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script1);
        await session.OpenAsync(script2);
        await session.OpenAsync(script3);

        await session.CloseAsync(script2.Id);
        await session.CloseAsync(script3.Id);

        Assert.Equal(script1, session.Active?.Script);
    }

    [Fact]
    public async Task ClosingNonActiveScript_DoesNotChangeActiveScript()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script1 = ScriptTestHelper.CreateScript();
        var script2 = ScriptTestHelper.CreateScript();
        var script3 = ScriptTestHelper.CreateScript();
        await session.OpenAsync(script2);
        await session.OpenAsync(script1);
        await session.OpenAsync(script3);

        await session.CloseAsync(script1.Id);

        Assert.Equal(script3, session.Active?.Script);
    }

    [Fact(Skip = "WIP")]
    public Task ClosingScript_DisposesItsEnvironment()
    {
        throw new NotImplementedException();
    }

    [Fact]
    public async Task OpeningScript_SetsItsEnvironmentScriptStatusToReady()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();

        await session.OpenAsync(script);

        Assert.Equal(ScriptStatus.Ready, session.Active?.Status);
    }
}

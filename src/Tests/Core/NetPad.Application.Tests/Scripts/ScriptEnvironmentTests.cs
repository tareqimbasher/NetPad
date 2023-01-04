using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NetPad.Data;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using NetPad.Tests.Services;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Application.Tests.Scripts;

public class ScriptEnvironmentTests : TestBase
{
    public ScriptEnvironmentTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddTransient<IDataConnectionResourcesCache, NullDataConnectionResourcesCache>();

        base.ConfigureServices(services);
    }

    [Fact]
    public void RunningScriptWhileItsAlreadyRunning_ThrowsInvalidOperationException()
    {
        var script = ScriptTestHelper.CreateScript();
        var environment = new Mock<ScriptEnvironment>(script, ServiceProvider.CreateScope());
        environment.Setup(e => e.Status).Returns(ScriptStatus.Running);

        Assert.ThrowsAsync<InvalidOperationException>(() => environment.Object.RunAsync(new RunOptions()));
    }

    [Fact]
    public async Task RunningAfterDisposingEnvironment_ThrowsInvalidOperationException()
    {
        var script = ScriptTestHelper.CreateScript();
        var environment = new ScriptEnvironment(script, ServiceProvider.CreateScope());

        environment.Dispose();

        await Assert.ThrowsAsync<InvalidOperationException>(() => environment.RunAsync(new RunOptions()));
    }
}

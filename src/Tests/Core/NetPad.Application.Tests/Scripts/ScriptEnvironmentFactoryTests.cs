using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Data;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using NetPad.Tests.Services;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Application.Tests.Scripts;

public class ScriptEnvironmentFactoryTests : TestBase
{
    public ScriptEnvironmentFactoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override void ConfigureServices(ServiceCollection services)
    {
        services.AddTransient<IDataConnectionResourcesCache, NullDataConnectionResourcesCache>();

        base.ConfigureServices(services);
    }

    [Fact]
    public async Task CreateEnvironment_CreatesEnvironmentSuccessfully()
    {
        IScriptEnvironmentFactory factory = new DefaultScriptEnvironmentFactory(ServiceProvider);
        var script = ScriptTestHelper.CreateScript();

        ScriptEnvironment environment = await factory.CreateEnvironmentAsync(script);

        Assert.NotNull(environment);
    }
}

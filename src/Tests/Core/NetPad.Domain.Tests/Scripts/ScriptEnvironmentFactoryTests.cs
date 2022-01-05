using System.Threading.Tasks;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Domain.Tests.Scripts
{
    public class ScriptEnvironmentFactoryTests : TestBase
    {
        public ScriptEnvironmentFactoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task CreateEnvironment_CreatesEnvironmentSuccessfully()
        {
            IScriptEnvironmentFactory factory = new ScriptEnvironmentFactory(ServiceProvider);
            var script = ScriptTestHelper.CreateScript();

            ScriptEnvironment environment = await factory.CreateEnvironmentAsync(script);

            Assert.NotNull(environment);
        }
    }
}

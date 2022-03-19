using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NetPad.IO;
using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Domain.Tests.Scripts
{
    public class ScriptEnvironmentTests : TestBase
    {
        public ScriptEnvironmentTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void SetIO_SetsIOCorrectly()
        {
            var script = ScriptTestHelper.CreateScript();
            var environment = new ScriptEnvironment(script, ServiceProvider.CreateScope());
            var inputReader = new ActionInputReader(() => null);
            var outputWriter = new ActionOutputWriter((o, title) => { });

            environment.SetIO(inputReader, outputWriter);

            object? getFieldValue(string name) => typeof(ScriptEnvironment)
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(environment);

            Assert.Equal(inputReader, getFieldValue("_inputReader"));
            Assert.Equal(outputWriter, getFieldValue("_outputWriter"));
        }

        [Fact]
        public void RunningScriptWhileItsAlreadyRunning_ThrowsInvalidOperationException()
        {
            var script = ScriptTestHelper.CreateScript();
            var environment = new Mock<ScriptEnvironment>(script, ServiceProvider.CreateScope());
            environment.Setup(e => e.Status).Returns(ScriptStatus.Running);

            Assert.ThrowsAsync<InvalidOperationException>(() => environment.Object.RunAsync());
        }

        [Fact]
        public async Task RunningAfterDisposingEnvironment_ThrowsInvalidOperationException()
        {
            var script = ScriptTestHelper.CreateScript();
            var environment = new ScriptEnvironment(script, ServiceProvider.CreateScope());

            environment.Dispose();

            await Assert.ThrowsAsync<InvalidOperationException>(() => environment.RunAsync());
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Scripts;
using NetPad.Sessions;
using NetPad.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Domain.Tests.Sessions
{
    public class SessionTests : TestBase
    {
        public SessionTests(ITestOutputHelper testOutputHelper): base(testOutputHelper)
        {
        }

        [Fact]
        public async Task GettingOpenedScriptById_ReturnsCorrectScript()
        {
            var session = GetNewSession();
            var script = GetNewScript();
            await session.OpenAsync(script);

            var result = session.Get(script.Id);

            Assert.Equal(script, result?.Script);
        }

        [Fact]
        public async Task GettingClosedScriptById_ReturnsNull()
        {
            var session = GetNewSession();
            var script = GetNewScript();
            await session.OpenAsync(script);
            await session.CloseAsync(script.Id);

            var result = session.Get(script.Id);

            Assert.Null(result);
        }

        [Fact]
        public void GettingNonOpenedScriptById_ReturnsNull()
        {
            var session = GetNewSession();

            var result = session.Get(Guid.NewGuid());

            Assert.Null(result);
        }


        [Fact]
        public void ActiveSession_IsNull_OnInitialization()
        {
            var session = GetNewSession();

            Assert.Null(session.Active);
        }

        [Fact]
        public async Task ActivingAScript_SetsItAsTheActiveScript()
        {
            var session = GetNewSession();
            var script1 = GetNewScript();
            var script2 = GetNewScript();
            await session.OpenAsync(script1);
            await session.OpenAsync(script2);

            await session.ActivateAsync(script1.Id);

            Assert.Equal(session.Active?.Script, script1);
        }

        [Fact]
        public async Task ActivatingLastActiveScript_ActivatesTheLastActiveScript()
        {
            var session = GetNewSession();
            var script1 = GetNewScript();
            var script2 = GetNewScript();
            await session.OpenAsync(script1);
            await session.OpenAsync(script2);
            await session.ActivateAsync(script1.Id);

            await session.ActivateLastActiveScriptAsync();

            Assert.Equal(session.Active?.Script, script2);
        }

        [Fact]
        public async Task ActivatingLastActiveScriptWhenNoScriptsAreOpen_DoesNotThrow()
        {
            var session = GetNewSession();

            await session.ActivateLastActiveScriptAsync();

            Assert.Null(session.Active);
        }

        [Fact]
        public async Task ActivatingLastActiveScriptWhenNoScriptWasLastActive_DoesNotChangeActiveProperty()
        {
            var session = GetNewSession();
            var script = GetNewScript();
            await session.OpenAsync(script);

            await session.ActivateLastActiveScriptAsync();

            Assert.Equal(session.Active?.Script, script);
        }

        [Fact]
        public async Task OpeningAScript_SetsItAsActive()
        {
            var session = GetNewSession();
            var script = GetNewScript();
            await session.OpenAsync(script);

            Assert.Equal(session.Active?.Script, script);
        }

        [Fact]
        public async Task OpeningAScript_AddsItToEnviornmentsCollection()
        {
            var session = GetNewSession();
            var script = GetNewScript();
            await session.OpenAsync(script);

            Assert.Equal(session.Environments.Single().Script, script);
        }

        [Fact]
        public async Task ClosingScript_RemovesItFromEnviornmentsCollection()
        {
            var session = GetNewSession();
            var script = GetNewScript();
            await session.OpenAsync(script);

            await session.CloseAsync(script.Id);

            Assert.Empty(session.Environments);
        }

        [Fact]
        public async Task ClosingActiveScript_WhenLastActiveScriptExists_MakesLastActiveScriptActive()
        {
            var session = GetNewSession();
            var script1 = GetNewScript();
            var script2 = GetNewScript();
            var script3 = GetNewScript();
            await session.OpenAsync(script2);
            await session.OpenAsync(script1);
            await session.OpenAsync(script3);

            await session.CloseAsync(script3.Id);

            Assert.Equal(script1, session.Active?.Script);
        }

        [Fact]
        public async Task ClosingLastActiveScript_SetsActiveToNull()
        {
            var session = GetNewSession();
            var script = GetNewScript();
            await session.OpenAsync(script);

            await session.CloseAsync(script.Id);

            Assert.Null(session.Active);
        }

        [Fact]
        public async Task ClosingActiveScript_WhenLastActiveScriptWasAlsoClosed_ActivatesScriptBeforeClosingActiveScript()
        {
            var session = GetNewSession();
            var script1 = GetNewScript();
            var script2 = GetNewScript();
            var script3 = GetNewScript();
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
            var session = GetNewSession();
            var script1 = GetNewScript();
            var script2 = GetNewScript();
            var script3 = GetNewScript();
            await session.OpenAsync(script2);
            await session.OpenAsync(script1);
            await session.OpenAsync(script3);

            await session.CloseAsync(script1.Id);

            Assert.Equal(script3, session.Active?.Script);
        }

        [Fact]
        public async Task GetsInitialSequentialNewScriptName_WhenNoScriptsAreOpened()
        {
            var session = GetNewSession();

            var name = await session.GetNewScriptNameAsync();

            Assert.Equal("Script 1", name);
        }

        [Fact]
        public async Task GetsNextSequentialNewScriptName_WhenOtherScriptsAlreadyOpen()
        {
            var session = GetNewSession();
            var script = GetNewScript(await session.GetNewScriptNameAsync());
            await session.OpenAsync(script);

            var name = await session.GetNewScriptNameAsync();

            Assert.Equal("Script 2", name);
        }

        [Fact]
        public async Task ClosingScript_DisposesItsEnvironment()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task OpeningScript_SetsItsEnvironmentScriptStatusToReady()
        {
            var session = GetNewSession();
            var script = GetNewScript();

            await session.OpenAsync(script);

            Assert.Equal(ScriptStatus.Ready, session.Active?.Status);
        }


        private Script GetNewScript(string? name = null)
        {
            var id = Guid.NewGuid();
            return new Script(id, name ?? $"Script {id}");
        }

        private Session GetNewSession()
        {
            return new Session(
                ServiceProvider,
                ServiceProvider.GetRequiredService<ILogger<Session>>());
        }
    }
}

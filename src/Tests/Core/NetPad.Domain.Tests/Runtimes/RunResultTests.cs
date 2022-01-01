using NetPad.Runtimes;
using NetPad.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Domain.Tests.Runtimes
{
    public class RunResultTests : TestBase
    {
        public RunResultTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void SuccessHelper_MarksRunAttemptAsSuccessful()
        {
            var result = RunResult.Success(0);

            Assert.True(result.IsRunAttemptSuccessful);
        }

        [Fact]
        public void SuccessHelper_MarksScriptCompletionAsSuccessful()
        {
            var result = RunResult.Success(0);

            Assert.True(result.IsScriptCompletedSuccessfully);
        }

        [Fact]
        public void SuccessHelper_SetsDuration()
        {
            var result = RunResult.Success(100);

            Assert.Equal(100, result.DurationMs);
        }

        [Fact]
        public void RunAttemptFailureHelper_MarksRunAttemptAsFailure()
        {
            var result = RunResult.RunAttemptFailure();

            Assert.False(result.IsRunAttemptSuccessful);
        }

        [Fact]
        public void RunAttemptFailureHelper_MarksScriptCompletionAsFailure()
        {
            var result = RunResult.RunAttemptFailure();

            Assert.False(result.IsScriptCompletedSuccessfully);
        }

        [Fact]
        public void RunAttemptFailureHelper_SetsDurationTo0()
        {
            var result = RunResult.RunAttemptFailure();

            Assert.Equal(0, result.DurationMs);
        }

        [Fact]
        public void ScriptCompletionFailureHelper_MarksRunAttemptAsSuccessful()
        {
            var result = RunResult.ScriptCompletionFailure();

            Assert.True(result.IsRunAttemptSuccessful);
        }

        [Fact]
        public void ScriptCompletionFailureHelper_MarksScriptCompletionAsFailure()
        {
            var result = RunResult.ScriptCompletionFailure();

            Assert.False(result.IsScriptCompletedSuccessfully);
        }

        [Fact]
        public void ScriptCompletionFailureHelper_SetsDurationTo0()
        {
            var result = RunResult.ScriptCompletionFailure();

            Assert.Equal(0, result.DurationMs);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Queries;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtimes
{
    public class QueryRuntimeConsoleTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public QueryRuntimeConsoleTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        public static IEnumerable<object[]> ConsoleOutputTestData => new[]
        {
            new[] {"\"Hello World\"", "Hello World"},
            new[] {"4 + 7", "11"},
            new[] {"4.7 * 2", "9.4"},
            new[] {"DateTime.Today", DateTime.Today.ToString()},
        };
        
        [Theory]
        [MemberData(nameof(ConsoleOutputTestData))]
        public async Task QueryRuntime_Redirects_Console_Output(string code, string expectedOutput)
        {
            var query = new Query("Console Output Test");
            query.Config.SetKind(QueryKind.Statements);
            query.UpdateCode($"Console.Write({code});");

            var runtime = new MainAppDomainQueryRuntime();
            await runtime.InitializeAsync(query);

            string? result = null;

            await runtime.RunAsync(
                null,
                new TestQueryRuntimeOutputReader(output => result = output?.ToString()));

            Assert.Equal(expectedOutput, result);
        }
    }
}
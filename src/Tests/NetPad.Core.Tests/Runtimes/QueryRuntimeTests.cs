using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Exceptions;
using NetPad.Queries;
using NetPad.Runtimes.Assemblies;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtimes
{
    public class QueryRuntimeTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public QueryRuntimeTests(ITestOutputHelper testOutputHelper)
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

        [Fact]
        public async Task Can_Not_Run_Expression_Kind_Query()
        {
            var query = new Query("Console Output Test");
            query.Config.SetKind(QueryKind.Expression);
            query.UpdateCode($@"Console.Write(5)");

            var runtime = GetQueryRuntime();
            await runtime.InitializeAsync(query);

            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await runtime.RunAsync(
                    null,
                    new TestQueryRuntimeOutputWriter(output => { }));
            });
        }

        [Theory]
        [MemberData(nameof(ConsoleOutputTestData))]
        public async Task Can_Run_Statements_Kind_Query(string code, string expectedOutput)
        {
            var query = new Query("Console Output Test");
            query.Config.SetKind(QueryKind.Statements);
            query.UpdateCode($"Console.Write({code});");

            var runtime = GetQueryRuntime();
            await runtime.InitializeAsync(query);

            string? result = null;

            await runtime.RunAsync(
                null,
                new TestQueryRuntimeOutputWriter(output => result = output?.ToString()));

            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [MemberData(nameof(ConsoleOutputTestData))]
        public async Task Can_Run_Program_Kind_Query(string code, string expectedOutput)
        {
            var query = new Query("Console Output Test");
            query.Config.SetKind(QueryKind.Program);
            query.UpdateCode($@"
void Main() {{
    Console.Write({code});
}}
");

            var runtime = GetQueryRuntime();
            await runtime.InitializeAsync(query);

            string? result = null;

            try
            {
                await runtime.RunAsync(
                    null,
                    new TestQueryRuntimeOutputWriter(output => result = output?.ToString()));
            }
            catch (CodeCompilationException ex)
            {
                _testOutputHelper.WriteLine(ex.ErrorsAsString());
                throw;
            }

            Assert.Equal(expectedOutput, result);
        }

        private IQueryRuntime GetQueryRuntime()
        {
            return new QueryRuntime(new MainAppDomainAssemblyLoader());
        }
    }
}

using NetPad.Scripts;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtime.Tests.Scripts;

public class DefaultScriptNameGeneratorTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper)
{
    [Fact]
    public void WhenSessionIsEmpty_GeneratesNameUsingDefaultBaseName()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var generator = new DefaultScriptNameGenerator(session);

        var name = generator.Generate();

        Assert.Equal("Script 1", name);
    }

    [Fact]
    public async Task WhenSessionIsNotEmpty_GeneratesNameUsingDefaultBaseName()
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var generator = new DefaultScriptNameGenerator(session);
        var script = ScriptTestHelper.CreateScript(name: generator.Generate());
        await session.OpenAsync(script, true);

        foreach (var num in Enumerable.Range(2, 15))
        {
            var name = generator.Generate();
            script = ScriptTestHelper.CreateScript(name: name);
            await session.OpenAsync(script, true);

            Assert.Equal($"Script {num}", name);
        }
    }

    [Theory]
    [InlineData("BaseName")]
    [InlineData("Base Name")]
    [InlineData("Base Name 1")]
    public void WhenSessionIsEmpty_GeneratesNameUsingProvidedBaseName(string baseName)
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var generator = new DefaultScriptNameGenerator(session);

        var name = generator.Generate(baseName);

        Assert.Equal($"{baseName} 1", name);
    }

    [Theory]
    [InlineData("BaseName")]
    [InlineData("Base Name")]
    [InlineData("Base Name 1")]
    public async Task WhenSessionIsNotEmpty_GeneratesNameUsingProvidedBaseName(string baseName)
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var generator = new DefaultScriptNameGenerator(session);
        var script = ScriptTestHelper.CreateScript(name: generator.Generate(baseName));
        await session.OpenAsync(script, true);

        foreach (var num in Enumerable.Range(2, 15))
        {
            var name = generator.Generate(baseName);
            script = ScriptTestHelper.CreateScript(name: name);
            await session.OpenAsync(script, true);

            Assert.Equal($"{baseName} {num}", name);
        }
    }

    [Theory]
    [InlineData(new[] { "BaseName" }, "BaseName", "BaseName 1")]
    [InlineData(new[] { "Base Name" }, "Base Name", "Base Name 1")]

    public async Task AddsNumberWhenExistingHaveNoNumbersAtEnd(string[] existingScriptNames, string baseName, string expectedNewScriptName)
    {
        await Run(existingScriptNames, baseName, expectedNewScriptName);
    }

    [Theory]
    [InlineData(new[] { "Base Name 1" }, "Base Name", "Base Name 2")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 1" }, "Base Name", "Base Name 2")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 2" }, "Base Name", "Base Name 2")]
    [InlineData(new[] { "Base Name 1 1" }, "Base Name", "Base Name 1")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 1" }, "Base Name 1 1", "Base Name 1 1 1")]
    public async Task IncrementsNumber(string[] existingScriptNames, string baseName, string expectedNewScriptName)
    {
        await Run(existingScriptNames, baseName, expectedNewScriptName);
    }

    [InlineData(new[] { "Base Name 1" }, "Base Name 1", "Base Name 1 1")]
    [InlineData(new[] { "Base Name 2" }, "Base Name 2", "Base Name 2 1")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 1" }, "Base Name 1", "Base Name 1 2")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 1 1" }, "Base Name 1 1", "Base Name 1 1 2")]
    [Theory]
    public async Task AddsNumberIfBaseNameEndsWithNumber(string[] existingScriptNames, string baseName, string expectedNewScriptName)
    {
        await Run(existingScriptNames, baseName, expectedNewScriptName);
    }

    [Theory]
    [InlineData(new[] { "Base Name 1", "Base Name 3" }, "Base Name", "Base Name 4")]
    [InlineData(new[] { "Base Name", "Base Name 3" }, "Base Name", "Base Name 4")]
    public async Task UsesNextHighestNumber(string[] existingScriptNames, string baseName, string expectedNewScriptName)
    {
        await Run(existingScriptNames, baseName, expectedNewScriptName);
    }

    private async Task Run(string[] existingScriptNames, string baseName, string expectedNewScriptName)
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var generator = new DefaultScriptNameGenerator(session);

        foreach (var existingScriptName in existingScriptNames)
        {
            var script = ScriptTestHelper.CreateScript();
            script.SetName(existingScriptName);
            await session.OpenAsync(script, true);
        }

        var newName = generator.Generate(baseName);

        Assert.Equal(expectedNewScriptName, newName);
    }
}

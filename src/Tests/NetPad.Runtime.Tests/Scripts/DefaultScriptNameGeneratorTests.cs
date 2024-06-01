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
        await session.OpenAsync(script);

        foreach (var num in Enumerable.Range(2, 15))
        {
            var name = generator.Generate();
            script = ScriptTestHelper.CreateScript(name: name);
            await session.OpenAsync(script);

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
        await session.OpenAsync(script);

        foreach (var num in Enumerable.Range(2, 15))
        {
            var name = generator.Generate(baseName);
            script = ScriptTestHelper.CreateScript(name: name);
            await session.OpenAsync(script);

            Assert.Equal($"{baseName} {num}", name);
        }
    }

    [Theory]
    // Adds number when no number at end
    [InlineData(new[] { "BaseName" }, "BaseName", "BaseName 1")]
    [InlineData(new[] { "Base Name" }, "Base Name", "Base Name 1")]

    // Increments number at the end
    [InlineData(new[] { "Base Name 1" }, "Base Name", "Base Name 2")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 1" }, "Base Name", "Base Name 2")]

    // Fills in available "number slots"
    [InlineData(new[] { "Base Name 1", "Base Name 3" }, "Base Name", "Base Name 2")]

    // Adds number even if base name ends with number
    [InlineData(new[] { "Base Name 1" }, "Base Name 1", "Base Name 1 1")]
    [InlineData(new[] { "Base Name 2" }, "Base Name 2", "Base Name 2 1")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 1" }, "Base Name 1", "Base Name 1 2")]
    [InlineData(new[] { "Base Name 1", "Base Name 1 1 1" }, "Base Name 1 1", "Base Name 1 1 2")]
    public async Task WhenSessionIsNotEmpty_GeneratesNameUsingProvidedBaseName2(string[] existingScriptNames, string baseName, string expectedNewScriptName)
    {
        var session = SessionTestHelper.CreateSession(ServiceProvider);
        var generator = new DefaultScriptNameGenerator(session);

        foreach (var existingScriptName in existingScriptNames)
        {
            var script = ScriptTestHelper.CreateScript();
            script.SetName(existingScriptName);
            await session.OpenAsync(script);
        }

        var newName = generator.Generate(baseName);

        Assert.Equal(expectedNewScriptName, newName);
    }
}

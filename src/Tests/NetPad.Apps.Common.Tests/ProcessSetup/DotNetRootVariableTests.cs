using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Apps.ProcessSetup;
using NetPad.DotNet;

namespace NetPad.Apps.Common.Tests.ProcessSetup;

[CollectionDefinition(Name, DisableParallelization = true)]
public class DotNetRootVariableCollection
{
    public const string Name = "Changes DOTNET_ROOT for the test process";
}

[Collection(DotNetRootVariableCollection.Name)]
public sealed class DotNetRootVariableTests : IDisposable
{
    private const string Unchanged = "/unchanged";
    private readonly string? _original = Environment.GetEnvironmentVariable(DotNetRootVariable.Name);
    private readonly Mock<IDotNetInfo> _dotNetInfo = new();

    public DotNetRootVariableTests()
    {
        Environment.SetEnvironmentVariable(DotNetRootVariable.Name, Unchanged);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(DotNetRootVariable.Name, _original);
    }

    [Fact]
    public void Points_DOTNET_ROOT_At_The_Installation_Found()
    {
        _dotNetInfo.Setup(i => i.LocateDotNetRootDirectory()).Returns("/found/dotnet");

        CreateStep().ApplyToCurrentProcess();

        Assert.Equal("/found/dotnet", Environment.GetEnvironmentVariable(DotNetRootVariable.Name));
    }

    [Fact]
    public void Leaves_DOTNET_ROOT_Alone_When_No_Installation_Is_Found()
    {
        _dotNetInfo.Setup(i => i.LocateDotNetRootDirectory()).Returns((string?)null);

        CreateStep().ApplyToCurrentProcess();

        Assert.Equal(Unchanged, Environment.GetEnvironmentVariable(DotNetRootVariable.Name));
    }

    [Fact]
    public void Does_Not_Throw_When_Looking_For_The_Installation_Fails()
    {
        _dotNetInfo.Setup(i => i.LocateDotNetRootDirectory()).Throws(new IOException("Search failed"));

        CreateStep().ApplyToCurrentProcess();

        Assert.Equal(Unchanged, Environment.GetEnvironmentVariable(DotNetRootVariable.Name));
    }

    private DotNetRootVariable CreateStep() => new(_dotNetInfo.Object, NullLogger<DotNetRootVariable>.Instance);
}

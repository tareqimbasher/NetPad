using System.Reflection;
using Moq;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.Data.Security;
using NetPad.DotNet;
using NetPad.DotNet.CodeAnalysis;
using NetPad.DotNet.References;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.Runtime.Tests.Compilation.Scripts.Dependencies;

public class ScriptDependencyResolverTests
{
    private static Script CreateScript(
        DotNetFrameworkVersion frameworkVersion = DotNetFrameworkVersion.DotNet9,
        DataConnection? dataConnection = null,
        params Reference[] references)
    {
        var script = ScriptTestHelper.CreateScript(frameworkVersion: frameworkVersion);

        foreach (var reference in references)
        {
            script.Config.References.Add(reference);
        }

        if (dataConnection != null)
        {
            script.SetDataConnection(dataConnection);
        }

        return script;
    }

    private static ScriptDependencyResolver CreateResolver(
        IDataConnectionResourcesCache? resourcesCache = null,
        IPackageProvider? packageProvider = null)
    {
        return new ScriptDependencyResolver(
            resourcesCache ?? new NullDataConnectionResourcesCache(),
            packageProvider ?? new NullPackageProvider());
    }

    private static DataConnection CreateMockDataConnection(Guid? id = null)
    {
        var mock = new Mock<DataConnection>(id ?? Guid.NewGuid(), "TestDb", DataConnectionType.PostgreSQL)
        {
            CallBase = true
        };
        mock.Setup(x => x.TestConnectionAsync(It.IsAny<IDataConnectionPasswordProtector>()))
            .ReturnsAsync(new DataConnectionTestResult(true));
        return mock.Object;
    }

    [Fact]
    public async Task ScriptWithNoRefsAndNoDataConnection_ReturnsBuiltInAssembliesOnly()
    {
        var script = CreateScript();
        var resolver = CreateResolver();

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        // Should contain at least the built-in user-visible assemblies (NetPad.Runtime, O2Html)
        Assert.NotEmpty(deps.References);
        Assert.All(deps.References, r => Assert.Equal(Dependant.Shared, r.Dependant));
        Assert.All(deps.References, r => Assert.Equal(DependencyLoadStrategy.DeployAndLoad, r.LoadStrategy));
        Assert.Empty(deps.Code);
    }

    [Fact]
    public async Task ScriptReferences_AreIncludedAsScriptDependant_WithLoadInPlace()
    {
        var assemblyRef = new AssemblyFileReference("/some/path/MyLib.dll");
        var script = CreateScript(references: assemblyRef);
        var resolver = CreateResolver();

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        var scriptRef = deps.References.FirstOrDefault(r =>
            r.Dependant == Dependant.Script && r.Reference == assemblyRef);
        Assert.NotNull(scriptRef);
        Assert.Equal(DependencyLoadStrategy.LoadInPlace, scriptRef.LoadStrategy);
    }

    [Fact]
    public async Task MultipleScriptReferences_AreAllIncluded()
    {
        var ref1 = new AssemblyFileReference("/path/Lib1.dll");
        var ref2 = new AssemblyFileReference("/path/Lib2.dll");
        var script = CreateScript(references: [ref1, ref2]);
        var resolver = CreateResolver();

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        var scriptRefs = deps.References.Where(r => r.Dependant == Dependant.Script).ToList();
        Assert.Equal(2, scriptRefs.Count);
        Assert.Contains(scriptRefs, r => r.Reference == ref1);
        Assert.Contains(scriptRefs, r => r.Reference == ref2);
    }

    [Fact]
    public async Task DataConnection_WithSourceCode_AddsCodeDependency()
    {
        var dataConnection = CreateMockDataConnection();
        var sourceCode = new DataConnectionSourceCode
        {
            ApplicationCode = new SourceCodeCollection([new SourceCode("public class DbContext {}")])
        };

        var resourcesCache = new Mock<IDataConnectionResourcesCache>();
        resourcesCache
            .Setup(c => c.GetResourcesAsync(dataConnection, It.IsAny<DotNetFrameworkVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataConnectionResources(dataConnection, DateTime.UtcNow) { SourceCode = sourceCode });

        var script = CreateScript(dataConnection: dataConnection);
        var resolver = CreateResolver(resourcesCache: resourcesCache.Object);

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        Assert.Single(deps.Code);
        Assert.Equal("public class DbContext {}", deps.Code[0].Code[0].Code.Value);
    }

    [Fact]
    public async Task DataConnection_WithAssembly_AddsSharedReferenceDependency()
    {
        var dataConnection = CreateMockDataConnection();
        var assemblyImage = new AssemblyImage(new AssemblyName("TestAssembly"), [0x00, 0x01, 0x02]);

        var resourcesCache = new Mock<IDataConnectionResourcesCache>();
        resourcesCache
            .Setup(c => c.GetResourcesAsync(dataConnection, It.IsAny<DotNetFrameworkVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataConnectionResources(dataConnection, DateTime.UtcNow) { Assembly = assemblyImage });

        var script = CreateScript(dataConnection: dataConnection);
        var resolver = CreateResolver(resourcesCache: resourcesCache.Object);

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        var assemblyRef = deps.References.FirstOrDefault(r =>
            r.Reference is AssemblyImageReference air && air.AssemblyImage == assemblyImage);
        Assert.NotNull(assemblyRef);
        Assert.Equal(Dependant.Shared, assemblyRef.Dependant);
        Assert.Equal(DependencyLoadStrategy.DeployAndLoad, assemblyRef.LoadStrategy);
    }

    [Fact]
    public async Task DataConnection_WithRequiredReferences_AddsSharedReferenceDependencies()
    {
        var dataConnection = CreateMockDataConnection();
        var requiredRef = new AssemblyFileReference("/provider/EFCore.dll");

        var resourcesCache = new Mock<IDataConnectionResourcesCache>();
        resourcesCache
            .Setup(c => c.GetResourcesAsync(dataConnection, It.IsAny<DotNetFrameworkVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataConnectionResources(dataConnection, DateTime.UtcNow)
            {
                RequiredReferences = [requiredRef]
            });

        var script = CreateScript(dataConnection: dataConnection);
        var resolver = CreateResolver(resourcesCache: resourcesCache.Object);

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        var providerRef = deps.References.FirstOrDefault(r => r.Reference == requiredRef);
        Assert.NotNull(providerRef);
        Assert.Equal(Dependant.Shared, providerRef.Dependant);
        Assert.Equal(DependencyLoadStrategy.DeployAndLoad, providerRef.LoadStrategy);
    }

    [Fact]
    public async Task NoDataConnection_DoesNotAddCodeOrDataConnectionReferences()
    {
        var script = CreateScript();
        var resolver = CreateResolver();

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        Assert.Empty(deps.Code);
        // All references should be the built-in Shared ones
        Assert.All(deps.References, r => Assert.Equal(Dependant.Shared, r.Dependant));
    }

    [Fact]
    public async Task DataConnection_WithEmptyResources_AddsNothingExtra()
    {
        var dataConnection = CreateMockDataConnection();
        var script = CreateScript(dataConnection: dataConnection);
        // NullDataConnectionResourcesCache returns empty resources
        var resolver = CreateResolver();

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        Assert.Empty(deps.Code);
        // Only script refs (none) + built-in shared refs
        Assert.DoesNotContain(deps.References, r => r.Dependant == Dependant.Script);
    }

    [Fact]
    public async Task Cancellation_ReturnsEarly()
    {
        var script = CreateScript();
        var resolver = CreateResolver();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var deps = await resolver.GetDependenciesAsync(script, cts.Token);

        // Should return whatever was collected before cancellation — at minimum, script references
        // The important thing is it doesn't throw
        Assert.NotNull(deps);
    }

    [Fact]
    public async Task BuiltInAssemblies_AreDeployAndLoad()
    {
        var script = CreateScript();
        var resolver = CreateResolver();

        var deps = await resolver.GetDependenciesAsync(script, CancellationToken.None);

        var builtIns = deps.References.Where(r =>
            r.Dependant == Dependant.Shared && r.Reference is AssemblyFileReference).ToList();
        Assert.NotEmpty(builtIns);
        Assert.All(builtIns, r => Assert.Equal(DependencyLoadStrategy.DeployAndLoad, r.LoadStrategy));
    }
}

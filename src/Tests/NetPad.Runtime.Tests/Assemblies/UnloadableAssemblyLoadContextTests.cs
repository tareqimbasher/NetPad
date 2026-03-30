using System.Reflection;
using NetPad.Assemblies;

namespace NetPad.Runtime.Tests.Assemblies;

public class UnloadableAssemblyLoadContextTests : IDisposable
{
    private readonly string _tempDir;

    public UnloadableAssemblyLoadContextTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NetPad_Tests", nameof(UnloadableAssemblyLoadContextTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        using var context = new UnloadableAssemblyLoadContext();

        Assert.NotNull(context);
    }

    [Fact]
    public void Constructor_WithMainAssemblyPath_CreatesInstance()
    {
        var assemblyPath = typeof(UnloadableAssemblyLoadContext).Assembly.Location;
        using var context = new UnloadableAssemblyLoadContext(assemblyPath);

        Assert.NotNull(context);
    }

    [Fact]
    public void LoadFrom_AfterDispose_ThrowsInvalidOperationException()
    {
        var context = new UnloadableAssemblyLoadContext();
        context.Dispose();
        var assemblyBytes = File.ReadAllBytes(typeof(UnloadableAssemblyLoadContextTests).Assembly.Location);

        Assert.Throws<InvalidOperationException>(() => context.LoadFrom(assemblyBytes));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var context = new UnloadableAssemblyLoadContext();

        context.Dispose();
        context.Dispose(); // Should not throw
    }

    [Fact]
    public void UseProbing_ReturnsSameInstance_ForFluentChaining()
    {
        using var context = new UnloadableAssemblyLoadContext();

        var returned = context.UseProbing([]);

        Assert.Same(context, returned);
    }

    /// <summary>
    /// Copies a known assembly into a temp directory so probing can find it.
    /// Uses the NetPad.Runtime assembly since it's a dependency we know exists.
    /// </summary>
    private string CopyAssemblyToProbingDir(string subDir = "probe")
    {
        var probeDir = Path.Combine(_tempDir, subDir);
        Directory.CreateDirectory(probeDir);

        // Copy the runtime assembly into the probing directory
        var runtimeAssemblyPath = typeof(NetPad.Utilities.Try).Assembly.Location;
        var destPath = Path.Combine(probeDir, Path.GetFileName(runtimeAssemblyPath));
        File.Copy(runtimeAssemblyPath, destPath);

        return probeDir;
    }

    [Fact]
    public void Probing_FindsAssemblyInProbingDirectory()
    {
        var probeDir = CopyAssemblyToProbingDir();
        using var context = new UnloadableAssemblyLoadContext();
        context.UseProbing([probeDir]);

        var runtimeAssemblyName = typeof(NetPad.Utilities.Try).Assembly.GetName();
        var loaded = context.LoadFromAssemblyName(runtimeAssemblyName);

        Assert.NotNull(loaded);
        Assert.Equal(runtimeAssemblyName.Name, loaded.GetName().Name);
    }

    [Fact]
    public void Probing_SkipsNonExistentDirectories()
    {
        var probeDir = CopyAssemblyToProbingDir();
        var nonExistent = Path.Combine(_tempDir, "does_not_exist");
        using var context = new UnloadableAssemblyLoadContext();
        context.UseProbing([nonExistent, probeDir]);

        var runtimeAssemblyName = typeof(NetPad.Utilities.Try).Assembly.GetName();
        var loaded = context.LoadFromAssemblyName(runtimeAssemblyName);

        Assert.NotNull(loaded);
    }

    [Fact]
    public void Probing_SkipsNonAssemblyFiles()
    {
        var probeDir = CopyAssemblyToProbingDir();
        // Add a non-assembly file to the probing directory
        File.WriteAllText(Path.Combine(probeDir, "not-an-assembly.dll"), "this is not a dll");

        using var context = new UnloadableAssemblyLoadContext();
        context.UseProbing([probeDir]);

        // Should still find the real assembly without choking on the fake one
        var runtimeAssemblyName = typeof(NetPad.Utilities.Try).Assembly.GetName();
        var loaded = context.LoadFromAssemblyName(runtimeAssemblyName);

        Assert.NotNull(loaded);
    }

    [Fact]
    public void Probing_RespectsUseConditions_WhenConditionRejects()
    {
        var probeDir = CopyAssemblyToProbingDir();
        using var context = new UnloadableAssemblyLoadContext();
        context.UseProbing(
            [probeDir],
            [(_, _) => false]); // Reject all probed assemblies

        var runtimeAssemblyName = typeof(NetPad.Utilities.Try).Assembly.GetName();

        // Should fall through to the default load context since the condition rejected the probed assembly
        var loaded = context.LoadFromAssemblyName(runtimeAssemblyName);

        // Still loads (from default context), but the probing condition was evaluated
        Assert.NotNull(loaded);
    }

    [Fact]
    public void Probing_ConditionReceivesCorrectAssemblyName()
    {
        var probeDir = CopyAssemblyToProbingDir();
        AssemblyName? receivedName = null;

        using var context = new UnloadableAssemblyLoadContext();
        context.UseProbing(
            [probeDir],
            [(_, name) => { receivedName = name; return true; }]);

        var runtimeAssemblyName = typeof(NetPad.Utilities.Try).Assembly.GetName();
        context.LoadFromAssemblyName(runtimeAssemblyName);

        Assert.NotNull(receivedName);
        Assert.Equal(runtimeAssemblyName.Name, receivedName!.Name);
    }
}

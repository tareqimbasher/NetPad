using Microsoft.CodeAnalysis;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.Scripts;
using Xunit;

namespace NetPad.Runtime.Tests.Scripts;

public class ScriptFingerprintTests
{
    private static ScriptFingerprint Make(
        string code,
        IEnumerable<string> namespaces,
        IEnumerable<Reference>? references = null,
        ScriptKind kind = ScriptKind.Program,
        DotNetFrameworkVersion targetFrameworkVersion = DotNetFrameworkVersion.DotNet9,
        OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
        bool useAspNet = false,
        Guid? dataConnectionId = null)
    {
        return ScriptFingerprint.Create(
            code: code,
            namespaces: namespaces,
            references: references ?? [],
            kind: kind,
            targetFrameworkVersion: targetFrameworkVersion,
            optimizationLevel: optimizationLevel,
            useAspNet: useAspNet,
            dataConnectionId: dataConnectionId
        );
    }

    [Fact]
    public void Equal_ForSameInputs()
    {
        var a = Make(
            code: "Console.WriteLine(\"Hi\");",
            namespaces: ["System", "System.IO"]);

        var b = Make(
            code: "Console.WriteLine(\"Hi\");",
            namespaces: ["System", "System.IO"]);

        Assert.Equal(a, b);
    }

    [Fact]
    public void CalculateHash_IsDeterministic_ForSameInputs()
    {
        var a = Make(
            code: "Console.WriteLine(\"Hi\");",
            namespaces: ["System", "System.IO"]);

        var b = Make(
            code: "Console.WriteLine(\"Hi\");",
            namespaces: ["System", "System.IO"]);

        Assert.Equal(a.CalculateHash(), b.CalculateHash());
    }

    [Fact]
    public void CalculateUuid_IsDeterministic_ForSameInputs()
    {
        var a = Make("x", ["System"]);
        var b = Make("x", ["System"]);

        Assert.Equal(a.CalculateUuid(), b.CalculateUuid());
    }

    [Fact]
    public void Changing_Code_Changes_Hash_And_Uuid()
    {
        var a = Make("Console.WriteLine(1);", ["System"]);
        var b = Make("Console.WriteLine(2);", ["System"]);

        Assert.NotEqual(a.CodeHash, b.CodeHash);
        Assert.NotEqual(a.CalculateHash(), b.CalculateHash());
        Assert.NotEqual(a.CalculateUuid(), b.CalculateUuid());
    }

    [Fact]
    public void Namespaces_Are_OrderInsensitive_And_Deduplicated()
    {
        var a = Make(
            code: "x",
            namespaces: ["System", " System.Linq ", "System", "System.IO"]);

        var b = Make(
            code: "x",
            namespaces: ["System.IO", "System.Linq", "System"]);

        // Same normalized namespaces hash
        Assert.Equal(a.NamespacesHash, b.NamespacesHash);

        // Since everything else is the same, full fingerprint hash/uuid should match too
        Assert.Equal(a.CalculateHash(), b.CalculateHash());
        Assert.Equal(a.CalculateUuid(), b.CalculateUuid());
    }

    [Fact]
    public void Toggling_ScriptKind_Changes_Hash_And_Uuid()
    {
        var a = Make("x", ["System"], kind: ScriptKind.Program);
        var b = Make("x", ["System"], kind: ScriptKind.SQL);

        Assert.NotEqual(a.CalculateHash(), b.CalculateHash());
        Assert.NotEqual(a.CalculateUuid(), b.CalculateUuid());
    }

    [Fact]
    public void Toggling_TargetFrameworkVersion_Changes_Hash_And_Uuid()
    {
        var a = Make("x", ["System"], targetFrameworkVersion: DotNetFrameworkVersion.DotNet8);
        var b = Make("x", ["System"], targetFrameworkVersion: DotNetFrameworkVersion.DotNet9);

        Assert.NotEqual(a.CalculateHash(), b.CalculateHash());
        Assert.NotEqual(a.CalculateUuid(), b.CalculateUuid());
    }

    [Fact]
    public void Toggling_OptimizationLevel_Changes_Hash_And_Uuid()
    {
        var a = Make("x", ["System"], optimizationLevel: OptimizationLevel.Debug);
        var b = Make("x", ["System"], optimizationLevel: OptimizationLevel.Release);

        Assert.NotEqual(a.CalculateHash(), b.CalculateHash());
        Assert.NotEqual(a.CalculateUuid(), b.CalculateUuid());
    }


    [Fact]
    public void Toggling_UseAspNet_Changes_Hash_And_Uuid()
    {
        var a = Make("x", ["System"], useAspNet: false);
        var b = Make("x", ["System"], useAspNet: true);

        Assert.NotEqual(a.CalculateHash(), b.CalculateHash());
        Assert.NotEqual(a.CalculateUuid(), b.CalculateUuid());
    }

    [Fact]
    public void Changing_DataConnectionId_Changes_Hash_And_Uuid()
    {
        var a = Make("x", ["System"], dataConnectionId: null);
        var b = Make("x", ["System"], dataConnectionId: Guid.NewGuid());

        Assert.NotEqual(a.CalculateHash(), b.CalculateHash());
        Assert.NotEqual(a.CalculateUuid(), b.CalculateUuid());
    }

    [Fact]
    public void CalculateUuid_Has_Rfc4122_Variant()
    {
        var f = Make("x", ["System"]);
        var uuid = f.CalculateUuid().ToString("D");
        var parts = uuid.Split('-');

        // Variant nibble is first hex of the 4th group; must be 8,9,a,b
        var nibble = char.ToLowerInvariant(parts[3][0]);
        Assert.Contains(nibble, new[] { '8', '9', 'a', 'b' });
    }

    [Fact]
    public void References_Are_OrderInsensitive_And_Deduplicated()
    {
        var pkgA1 = new PackageReference("Newtonsoft.Json", "Json.NET", "13.0.3");
        var pkgA2 = new PackageReference("Newtonsoft.Json", "Json.NET", "13.0.3"); // duplicate
        var pkgB = new PackageReference("Dapper", "Dapper", "2.1.24");

        var fileRef1 = new AssemblyFileReference("/test/path");
        var fileRef2 = new AssemblyFileReference("/test/path"); // duplicate

        var a = Make(
            code: "x",
            namespaces: ["System"],
            references: [pkgA1, pkgB, fileRef1, pkgA2, fileRef2] // messy, dupes, mixed order
        );

        var b = Make(
            code: "x",
            namespaces: ["System"],
            references: [fileRef1, pkgB, pkgA1] // unique, different order
        );

        Assert.Equal(a.ReferencesHash, b.ReferencesHash);
        Assert.Equal(a.CalculateHash(), b.CalculateHash());
        Assert.Equal(a.CalculateUuid(), b.CalculateUuid());
    }
}

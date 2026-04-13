using System.Net;
using NetPad.Apps.Mcp.Dtos;
using NetPad.Apps.Mcp.Tests.Helpers;
using NetPad.Apps.Mcp.Tools;

namespace NetPad.Apps.Mcp.Tests.Tools;

public class UpdateScriptReferencesToolTests
{
    private static (NetPadApiClient Client, MockHttpMessageHandler Handler) CreateClient()
        => ApiClientTestHelper.CreateClient();

    private static ScriptDto ScriptWithRefs(Guid id, params ReferenceDto[] refs)
    {
        return new ScriptDto
        {
            Id = id,
            Name = "Test",
            Config = new ScriptConfigDto { References = refs }
        };
    }

    private static ReferenceDto PackageRef(string packageId, string version) => new()
    {
        Discriminator = ReferenceDto.PackageReferenceDiscriminator,
        Title = packageId,
        PackageId = packageId,
        Version = version
    };

    private static ReferenceDto AssemblyRef(string path) => new()
    {
        Discriminator = ReferenceDto.AssemblyFileReferenceDiscriminator,
        Title = Path.GetFileName(path),
        AssemblyPath = path
    };

    [Fact]
    public async Task InvalidScriptId_ReturnsError()
    {
        var (client, _) = CreateClient();

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, "bad-id", addPackages: [new PackageInput { Id = "X", Version = "1.0" }]);

        Assert.Equal("Invalid scriptId format. Expected a GUID.", result);
    }

    [Fact]
    public async Task NoChangesSpecified_ReturnsError()
    {
        var (client, _) = CreateClient();

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, Guid.NewGuid().ToString());

        Assert.Contains("No changes specified", result);
    }

    [Fact]
    public async Task AddPackage_WithExplicitVersion_AddsToReferences()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId));
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/references", HttpStatusCode.NoContent);

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(),
            addPackages: [new PackageInput { Id = "Dapper", Version = "2.1.0" }]);

        Assert.Contains("added Dapper v2.1.0", result);
        var putRequest = handler.Requests.First(r => r.Method == HttpMethod.Put);
        Assert.Contains("Dapper", putRequest.Body);
        Assert.Contains("2.1.0", putRequest.Body);
    }

    [Fact]
    public async Task AddPackage_WithoutVersion_ResolvesLatest()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.SetupRaw(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK,
            "[\"1.0.0\",\"2.0.0\"]");
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId));
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/references", HttpStatusCode.NoContent);

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(),
            addPackages: [new PackageInput { Id = "SomePackage" }]);

        Assert.Contains("added SomePackage v2.0.0", result);
    }

    [Fact]
    public async Task AddPackage_EmptyId_ReturnsValidationError()
    {
        var (client, _) = CreateClient();

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, Guid.NewGuid().ToString(),
            addPackages: [new PackageInput { Id = "" }]);

        Assert.Contains("'id' field", result);
    }

    [Fact]
    public async Task AddPackage_UnknownPackage_ReturnsResolutionError()
    {
        var (client, handler) = CreateClient();
        handler.SetupRaw(HttpMethod.Get, "/packages/versions", HttpStatusCode.OK, "[]");

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, Guid.NewGuid().ToString(),
            addPackages: [new PackageInput { Id = "NonExistent" }]);

        Assert.Contains("Could not resolve versions", result);
        Assert.Contains("NonExistent", result);
    }

    [Fact]
    public async Task RemovePackage_ExistingPackage_RemovesFromReferences()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId, PackageRef("Dapper", "2.1.0")));
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/references", HttpStatusCode.NoContent);

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(), removePackages: ["Dapper"]);

        Assert.Contains("removed 1 package(s)", result);
        var putRequest = handler.Requests.First(r => r.Method == HttpMethod.Put);
        Assert.DoesNotContain("Dapper", putRequest.Body);
    }

    [Fact]
    public async Task RemovePackage_NonExistent_NoChangesNeeded()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId));

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(), removePackages: ["NonExistent"]);

        Assert.Contains("No changes needed", result);
    }

    [Fact]
    public async Task AddAssembly_AddsToReferences()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId));
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/references", HttpStatusCode.NoContent);

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(), addAssemblies: ["/path/to/MyLib.dll"]);

        Assert.Contains("added assembly MyLib.dll", result);
        var putRequest = handler.Requests.First(r => r.Method == HttpMethod.Put);
        Assert.Contains("AssemblyFileReference", putRequest.Body);
        Assert.Contains("/path/to/MyLib.dll", putRequest.Body);
    }

    [Fact]
    public async Task AddAssembly_AlreadyExists_NoChangesNeeded()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId, AssemblyRef("/path/to/MyLib.dll")));

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(), addAssemblies: ["/path/to/MyLib.dll"]);

        Assert.Contains("No changes needed", result);
    }

    [Fact]
    public async Task RemoveAssembly_ExistingAssembly_RemovesFromReferences()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId, AssemblyRef("/path/to/MyLib.dll")));
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/references", HttpStatusCode.NoContent);

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(), removeAssemblies: ["/path/to/MyLib.dll"]);

        Assert.Contains("removed 1 assembly reference(s)", result);
    }

    [Fact]
    public async Task UpdatePackageVersion_ExistingPackage_UpdatesVersion()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId, PackageRef("Dapper", "2.0.0")));
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/references", HttpStatusCode.NoContent);

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(),
            addPackages: [new PackageInput { Id = "Dapper", Version = "2.1.0" }]);

        Assert.Contains("updated Dapper to v2.1.0", result);
    }

    [Fact]
    public async Task AddPackage_SameVersionAlreadyExists_NoChangesNeeded()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId, PackageRef("Dapper", "2.1.0")));

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(),
            addPackages: [new PackageInput { Id = "Dapper", Version = "2.1.0" }]);

        Assert.Contains("No changes needed", result);
    }

    [Fact]
    public async Task CombinedAddAndRemove_AppliesBothChanges()
    {
        var (client, handler) = CreateClient();
        var scriptId = Guid.NewGuid();
        handler.Setup(HttpMethod.Get, $"/scripts/{scriptId}", HttpStatusCode.OK,
            ScriptWithRefs(scriptId, PackageRef("OldPackage", "1.0.0")));
        handler.Setup(HttpMethod.Put, $"/scripts/{scriptId}/references", HttpStatusCode.NoContent);

        var result = await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, scriptId.ToString(),
            addPackages: [new PackageInput { Id = "NewPackage", Version = "3.0.0" }],
            removePackages: ["OldPackage"]);

        Assert.Contains("removed 1 package(s)", result);
        Assert.Contains("added NewPackage v3.0.0", result);
        var putRequest = handler.Requests.First(r => r.Method == HttpMethod.Put);
        Assert.DoesNotContain("OldPackage", putRequest.Body);
        Assert.Contains("NewPackage", putRequest.Body);
    }

    [Fact]
    public async Task ValidationFailsBeforeApiCalls_NoRequestsMade()
    {
        var (client, handler) = CreateClient();

        await UpdateScriptReferencesTool.UpdateScriptReferences(
            client, "bad-id", addPackages: [new PackageInput { Id = "X", Version = "1.0" }]);

        Assert.Empty(handler.Requests);
    }
}

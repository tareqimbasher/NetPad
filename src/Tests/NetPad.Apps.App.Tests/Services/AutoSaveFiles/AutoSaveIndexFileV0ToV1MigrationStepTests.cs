using System.Text.Json.Nodes;
using NetPad.Services.AutoSaveFiles;

namespace NetPad.Apps.App.Tests.Services.AutoSaveFiles;

public class AutoSaveIndexFileV0ToV1MigrationStepTests
{
    [Fact]
    public void Migrates_Legacy_Guid_To_Name_Map_To_Versioned_Entries()
    {
        var step = new AutoSaveIndexFileV0ToV1MigrationStep();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var doc = JsonNode.Parse($$"""{"{{id1}}": "First", "{{id2}}": "Second"}""") as JsonObject;

        step.Apply(doc!);

        Assert.Equal(1, (int?)doc!["version"]);
        var entries = doc["entries"] as JsonObject;
        Assert.NotNull(entries);
        Assert.Equal(2, entries!.Count);
        Assert.Equal("First", (string?)entries[id1.ToString()]!["name"]);
        Assert.Null((string?)entries[id1.ToString()]!["originalPath"]);
        Assert.Equal("Second", (string?)entries[id2.ToString()]!["name"]);
    }

    [Fact]
    public void Migrates_Empty_Document_To_Versioned_Empty_Entries()
    {
        var step = new AutoSaveIndexFileV0ToV1MigrationStep();
        var doc = JsonNode.Parse("{}") as JsonObject;

        step.Apply(doc!);

        Assert.Equal(1, (int?)doc!["version"]);
        Assert.Empty((doc["entries"] as JsonObject)!);
    }

    [Fact]
    public void From_Version_Is_0_And_To_Version_Is_1()
    {
        var step = new AutoSaveIndexFileV0ToV1MigrationStep();
        Assert.Equal(0, step.FromVersion);
        Assert.Equal(1, step.ToVersion);
    }
}

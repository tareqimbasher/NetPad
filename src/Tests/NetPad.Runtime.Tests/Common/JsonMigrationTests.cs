using System.Text.Json;
using System.Text.Json.Nodes;
using NetPad.Common;

namespace NetPad.Runtime.Tests.Common;

public class JsonMigrationTests
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void CanMigrateFromV0ToV1()
    {
        var pipeline = new JsonMigrationPipeline([new V0ToV1MigrationStep(), new V1ToV2MigrationStep()]);

        var v1 = pipeline.MigrateTo<V1Model>(V0Json, 1, _serializerOptions);

        Assert.Equal(1, v1.Version);
        Assert.Equal("John Doe", v1.Name);
        Assert.Equal(30, v1.Age);
    }

    [Fact]
    public void CanMigrateFromV1ToV2()
    {
        var pipeline = new JsonMigrationPipeline([new V0ToV1MigrationStep(), new V1ToV2MigrationStep()]);

        var v2 = pipeline.MigrateTo<V2Model>(V1Json, 2, _serializerOptions);

        Assert.Equal(2, v2.Version);
        Assert.Equal("John Doe", v2.FullName);
        Assert.Equal(30, v2.Age);
        Assert.Equal("Male", v2.Gender);
    }

    [Fact]
    public void CanMigrateFromV0ToV2()
    {
        var pipeline = new JsonMigrationPipeline([new V0ToV1MigrationStep(), new V1ToV2MigrationStep()]);

        var v2 = pipeline.MigrateTo<V2Model>(V0Json, 2, _serializerOptions);

        Assert.Equal(2, v2.Version);
        Assert.Equal("John Doe", v2.FullName);
        Assert.Equal(30, v2.Age);
        Assert.Equal("Male", v2.Gender);
    }

    [Fact]
    public void CanMigrateFromV0ToLatest()
    {
        var pipeline = new JsonMigrationPipeline([new V0ToV1MigrationStep(), new V1ToV2MigrationStep()]);

        var latest = pipeline.MigrateToLatest<V2Model>(V0Json, _serializerOptions);

        Assert.Equal(2, latest.Version);
        Assert.Equal("John Doe", latest.FullName);
        Assert.Equal(30, latest.Age);
        Assert.Equal("Male", latest.Gender);
    }

    [Fact]
    public void ReturnsV2WhenInputIsV2()
    {
        var pipeline = new JsonMigrationPipeline([new V0ToV1MigrationStep(), new V1ToV2MigrationStep()]);

        var v2 = pipeline.MigrateToLatest<V2Model>(V2Json, _serializerOptions);

        Assert.Equal(2, v2.Version);
        Assert.Equal("John Doe", v2.FullName);
        Assert.Equal(30, v2.Age);
        Assert.Equal("Male", v2.Gender);
    }

    private const string V0Json =
        """
        {
            "firstName": "John",
            "lastName": "Doe",
            "age": 30
        }
        """;

    private const string V1Json =
        """
        {
            "version": 1,
            "name": "John Doe",
            "age": 30
        }
        """;

    class V1Model : IVersionedJson
    {
        public int Version => 1;
        public required string Name { get; set; }
        public required int Age { get; set; }
    }

    public sealed class V0ToV1MigrationStep : IJsonMigrationStep
    {
        public int FromVersion => 0;
        public int ToVersion => 1;

        public void Apply(JsonObject doc)
        {
            var first = doc["firstName"]?.GetValue<string>();
            var last = doc["lastName"]?.GetValue<string>();

            doc["name"] = $"{first} {last}".Trim();

            doc.Remove("firstName");
            doc.Remove("lastName");

            doc["version"] = 1;
        }
    }

    private const string V2Json =
        """
        {
            "version": 2,
            "fullName": "John Doe",
            "age": 30,
            "gender": "Male"
        }
        """;

    class V2Model : IVersionedJson
    {
        public int Version => 2;
        public required string FullName { get; set; }
        public required int Age { get; set; }
        public required string Gender { get; set; }
    }

    public sealed class V1ToV2MigrationStep : IJsonMigrationStep
    {
        public int FromVersion => 1;
        public int ToVersion => 2;

        public void Apply(JsonObject doc)
        {
            doc["fullName"] = doc["name"]?.GetValue<string>() ?? "";
            doc["gender"] ??= "Male";
            doc["version"] = 2;
        }
    }
}

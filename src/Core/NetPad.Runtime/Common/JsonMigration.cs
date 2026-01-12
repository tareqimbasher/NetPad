using System.Text.Json;
using System.Text.Json.Nodes;

namespace NetPad.Common;

/// <summary>
/// Represents a strongly typed model that is backed by a versioned JSON document.
/// </summary>
/// <remarks>
/// All models produced by <see cref="JsonMigrationPipeline"/> must implement
/// this interface so their schema version can be validated after deserialization.
/// </remarks>
public interface IVersionedJson
{
    /// <summary>
    /// Gets the JSON schema version the model represents.
    /// </summary>
    /// <remarks>
    /// Should not implement a setter. It is safer to implement a getter only with a literal value.
    /// Example: public int Version => 1;
    /// </remarks>
    int Version { get; }
}

/// <summary>
/// Represents a single JSON migration step that transforms a document
/// from one schema version to the next sequential version.
/// </summary>
/// <remarks>
/// Implementations must migrate strictly from <see cref="FromVersion"/> to
/// <see cref="ToVersion"/> where <c>ToVersion = FromVersion + 1</c>.
/// The migration should mutate the provided <see cref="JsonObject"/> in place
/// and update its <c>version</c> property accordingly.
/// </remarks>
public interface IJsonMigrationStep
{
    /// <summary>
    /// Gets the source schema version that this migration step expects.
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// Gets the target schema version that this migration step produces.
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    /// Applies the migration to the specified JSON document.
    /// </summary>
    /// <param name="doc">
    /// The root JSON object to migrate. Implementations should mutate this
    /// object in place and advance its <c>version</c> property.
    /// </param>
    void Apply(JsonObject doc);
}

/// <summary>
/// Provides a pipeline for migrating versioned JSON documents through a
/// sequence of migration steps.
/// </summary>
public sealed class JsonMigrationPipeline
{
    private readonly IReadOnlyList<IJsonMigrationStep> _steps;

    /// <summary>Gets the minimum schema version supported by this pipeline.</summary>
    public int MinVersion { get; }

    /// <summary>Gets the latest (maximum) schema version supported by this pipeline.</summary>
    public int LatestVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMigrationPipeline"/> class.
    /// </summary>
    /// <param name="steps">
    /// The ordered set of migration steps forming a contiguous version chain.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when no migration steps are provided.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the migration steps do not form a valid contiguous N → N+1 chain.
    /// </exception>
    public JsonMigrationPipeline(IEnumerable<IJsonMigrationStep> steps)
    {
        _steps = steps
            .OrderBy(s => s.FromVersion)
            .ToArray();

        if (_steps.Count == 0)
        {
            throw new ArgumentException("At least one migration step is required.");
        }

        // Validate chain is continuous without gaps or overlaps
        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _steps[i];

            if (step.ToVersion != step.FromVersion + 1)
            {
                throw new InvalidOperationException(
                    $"{step.GetType().Name} must be a +1 step (N → N+1). Got {step.FromVersion} → {step.ToVersion}.");
            }

            if (i > 0 && _steps[i - 1].ToVersion != step.FromVersion)
            {
                throw new InvalidOperationException(
                    $"Migration chain has a gap or overlap between versions {_steps[i - 1].ToVersion} and {step.FromVersion}.");
            }
        }

        MinVersion = _steps[0].FromVersion;
        LatestVersion = _steps[^1].ToVersion;
    }

    /// <summary>
    /// Migrates the specified JSON document to the latest supported schema version
    /// and deserializes it into the corresponding model type.
    /// </summary>
    /// <typeparam name="TLatest">
    /// The model type representing the latest schema version.
    /// </typeparam>
    /// <param name="json">The input JSON document.</param>
    /// <param name="options">
    /// Optional JSON serializer options used during deserialization.
    /// </param>
    /// <returns>
    /// An instance of <typeparamref name="TLatest"/> representing the migrated document.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the document cannot be migrated or deserialized correctly.
    /// </exception>
    public TLatest MigrateToLatest<TLatest>(string json, JsonSerializerOptions? options = null)
        where TLatest : IVersionedJson
    {
        return MigrateTo<TLatest>(json, LatestVersion, options);
    }

    /// <summary>
    /// Migrates the specified JSON document to the specified schema version
    /// and deserializes it into the corresponding model type.
    /// </summary>
    /// <typeparam name="TModel">
    /// The model type representing the required schema version.
    /// </typeparam>
    /// <param name="json">The input JSON document.</param>
    /// <param name="requiredVersion">
    /// The target schema version to migrate the document to.
    /// </param>
    /// <param name="options">
    /// Optional JSON serializer options used during deserialization.
    /// </param>
    /// <returns>
    /// An instance of <typeparamref name="TModel"/> representing the migrated document.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the required version is outside the supported version range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the document cannot be migrated or deserialized correctly.
    /// </exception>
    public TModel MigrateTo<TModel>(string json, int requiredVersion, JsonSerializerOptions? options = null)
        where TModel : IVersionedJson
    {
        if (requiredVersion < MinVersion || requiredVersion > LatestVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredVersion),
                $"Required version {requiredVersion} is out of range. Supported versions: {MinVersion} - {LatestVersion}.");
        }

        var doc = JsonNode.Parse(json) as JsonObject
                  ?? throw new InvalidOperationException("Root JSON must be an object.");

        int version = GetVersionOrDefault(doc);

        if (version > requiredVersion)
        {
            throw new InvalidOperationException(
                $"File version {version} is newer than the required version: {requiredVersion}.");
        }

        while (version < requiredVersion)
        {
            var step = _steps.FirstOrDefault(s => s.FromVersion == version)
                       ?? throw new InvalidOperationException(
                           $"Missing migration step for version {version} → {version + 1}.");

            step.Apply(doc);

            // Ensure the step actually advanced
            int newVersion = GetVersionOrDefault(doc);
            if (newVersion != version + 1)
            {
                throw new InvalidOperationException(
                    $"{step.GetType().Name} did not advance version from {version} to {version + 1} (found {newVersion}).");
            }

            version = newVersion;
        }

        var model = doc.Deserialize<TModel>(options)
                    ?? throw new InvalidOperationException(
                        $"Deserialization into version {requiredVersion} model returned null.");

        if (model.Version != requiredVersion)
        {
            throw new InvalidOperationException(
                $"Deserialized model did not have the required version {requiredVersion} (found {model.Version}).");
        }

        return model;
    }

    private static int GetVersionOrDefault(JsonObject doc)
    {
        if (!doc.TryGetPropertyValue("version", out var node) || node is null)
        {
            return 0;
        }

        if (node is JsonValue val && val.TryGetValue<int>(out var version))
        {
            return version;
        }

        throw new InvalidOperationException($"Invalid 'version' value: {node.ToJsonString()}");
    }
}

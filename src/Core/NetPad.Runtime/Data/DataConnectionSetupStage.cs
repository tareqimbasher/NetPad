namespace NetPad.Data;

/// <summary>
/// A step in preparing a data connection for use by scripts, in the order the steps run. Stages are reported as each
/// one starts.
/// </summary>
public enum DataConnectionSetupStage
{
    /// <summary>
    /// Checking whether the database schema changed since the connection's cached resources were generated.
    /// </summary>
    CheckingSchema,

    /// <summary>
    /// Adding the connection's EF Core packages when the provider package is not on disk yet.
    /// </summary>
    DownloadingPackages,

    /// <summary>
    /// Adding the connection's EF Core packages when the provider package is already on disk.
    /// </summary>
    RestoringPackages,

    /// <summary>
    /// Generating the data context and entity code from the database.
    /// </summary>
    Scaffolding,

    /// <summary>
    /// Generating the compiled model of the data context.
    /// </summary>
    OptimizingModel,

    /// <summary>
    /// Compiling the scaffolded code into the data context assembly.
    /// </summary>
    Compiling,

    /// <summary>
    /// Reading the tables, columns and relationships of the database.
    /// </summary>
    ReadingStructure,
}

public static class DataConnectionSetupStageExtensions
{
    /// <summary>
    /// Gets the text that tells the user which stage is running.
    /// </summary>
    public static string GetDescription(this DataConnectionSetupStage stage) => stage switch
    {
        DataConnectionSetupStage.CheckingSchema => "Checking database for schema changes...",
        DataConnectionSetupStage.DownloadingPackages => "Downloading EF Core packages...",
        DataConnectionSetupStage.RestoringPackages => "Restoring EF Core packages...",
        DataConnectionSetupStage.Scaffolding => "Scaffolding database...",
        DataConnectionSetupStage.OptimizingModel => "Optimizing data context...",
        DataConnectionSetupStage.Compiling => "Compiling data context...",
        DataConnectionSetupStage.ReadingStructure => "Reading database structure...",
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
}

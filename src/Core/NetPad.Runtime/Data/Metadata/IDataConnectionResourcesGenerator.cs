using NetPad.DotNet;

namespace NetPad.Data.Metadata;

/// <summary>
/// Generates the resources (assembly, code...etc.) needed to use data connections.
/// </summary>
public interface IDataConnectionResourcesGenerator
{
    /// <summary>
    /// Generates the resources (assembly, code...etc.) needed to use a <see cref="DataConnection"/>.
    /// </summary>
    /// <param name="dataConnection">The target data connection.</param>
    /// <param name="targetFrameworkVersion">The target .NET framework version.</param>
    /// <param name="reportStage">
    /// Called synchronously, in order, as each stage of the generation starts. It must return quickly and not block.
    /// </param>
    Task<DataConnectionResources> GenerateResourcesAsync(
        DataConnection dataConnection,
        DotNetFrameworkVersion targetFrameworkVersion,
        Action<DataConnectionSetupStage>? reportStage = null);
}

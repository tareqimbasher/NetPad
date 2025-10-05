using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.Packages;

namespace NetPad.Compilation.Scripts.Dependencies;

/// <summary>
/// A reference (ex. assembly or nuget package) required to run a script.
/// </summary>
/// <param name="Reference">The reference required by the script to run.</param>
/// <param name="Dependant">Which components need this dependency.</param>
/// <param name="LoadStrategy">The manner in which this dependency will be deployed and loaded.</param>
public record ReferenceDependency(Reference Reference, Dependant Dependant, DependencyLoadStrategy LoadStrategy)
{
    public ReferenceAsset[] Assets { get; private set; } = [];

    public async Task LoadAssetsAsync(
        DotNetFrameworkVersion dotNetFrameworkVersion,
        IPackageProvider packageProvider,
        CancellationToken cancellationToken = default)
    {
        var assets = await Reference.GetAssetsAsync(dotNetFrameworkVersion, packageProvider, cancellationToken);
        Assets = assets.ToArray();
    }
}

using System;

namespace OmniSharp
{
    /// <summary>
    /// The .NET SDK an OmniSharp process should use. The version must name an SDK inside the root directory,
    /// or OmniSharp doesn't start.
    /// </summary>
    public sealed class OmniSharpDotNetSdk
    {
        /// <param name="rootDirectoryPath">The .NET root directory, which holds the <c>dotnet</c> executable and the <c>sdk</c> folder.</param>
        /// <param name="version">The exact SDK version (e.g. 8.0.416), or null to let OmniSharp use the newest SDK it finds.</param>
        public OmniSharpDotNetSdk(string rootDirectoryPath, string? version = null)
        {
            RootDirectoryPath = rootDirectoryPath ?? throw new ArgumentNullException(nameof(rootDirectoryPath));
            Version = version;
        }

        public string RootDirectoryPath { get; }

        public string? Version { get; }
    }
}

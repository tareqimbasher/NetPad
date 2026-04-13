using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

internal static class PackageReferenceHelper
{
    public static string? ValidatePackageIds(PackageInput[] packages)
    {
        foreach (var pkg in packages)
        {
            if (string.IsNullOrWhiteSpace(pkg.Id))
            {
                return "Each package must have an 'id' field.";
            }
        }

        return null;
    }

    public static async Task<(PackageInput[] resolved, string? error)> ResolveVersionsAsync(
        NetPadApiClient api,
        PackageInput[] packages,
        CancellationToken cancellationToken)
    {
        var validationError = ValidatePackageIds(packages);
        if (validationError != null)
        {
            return (packages, validationError);
        }

        var needsResolution = packages.Where(p => string.IsNullOrWhiteSpace(p.Version)).ToArray();

        if (needsResolution.Length == 0)
        {
            return (packages, null);
        }

        var resolutionTasks = needsResolution.Select(async pkg =>
        {
            var versions = await api.GetPackageVersionsAsync(pkg.Id, cancellationToken: cancellationToken);
            return (Package: pkg, Versions: versions);
        });

        var results = await Task.WhenAll(resolutionTasks);

        var errors = new List<string>();

        foreach (var (package, versions) in results)
        {
            if (versions.Length == 0)
            {
                errors.Add(package.Id);
            }
            else
            {
                package.Version = versions[^1];
            }
        }

        if (errors.Count > 0)
        {
            return (packages, $"Could not resolve versions for: {string.Join(", ", errors)}. " +
                              "Use search_packages to verify the package IDs.");
        }

        return (packages, null);
    }

    public static ReferenceDto CreatePackageRef(PackageInput pkg) => new()
    {
        Discriminator = ReferenceDto.PackageReferenceDiscriminator,
        Title = pkg.Id,
        PackageId = pkg.Id,
        Version = pkg.Version
    };

    public static ReferenceDto CreateAssemblyRef(string assemblyPath) => new()
    {
        Discriminator = ReferenceDto.AssemblyFileReferenceDiscriminator,
        Title = Path.GetFileName(assemblyPath),
        AssemblyPath = assemblyPath
    };

    public static List<ReferenceDto> BuildReferenceDtos(PackageInput[]? packages, string[]? assemblyPaths)
    {
        var refs = new List<ReferenceDto>();

        if (packages is { Length: > 0 })
        {
            foreach (var pkg in packages)
            {
                refs.Add(CreatePackageRef(pkg));
            }
        }

        if (assemblyPaths is { Length: > 0 })
        {
            foreach (var asmPath in assemblyPaths)
            {
                refs.Add(CreateAssemblyRef(asmPath));
            }
        }

        return refs;
    }
}

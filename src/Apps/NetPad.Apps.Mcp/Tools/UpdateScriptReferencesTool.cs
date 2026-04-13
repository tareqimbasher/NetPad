using System.ComponentModel;
using ModelContextProtocol.Server;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class UpdateScriptReferencesTool
{
    [McpServerTool(Name = "update_script_references", Destructive = false, Idempotent = true), Description(
         "Add or remove NuGet package references and assembly file references on an open script. " +
         "Use search_packages to find available packages and get_package_versions to find available versions if needed. " +
         "If a package version is omitted, the latest stable version will be resolved automatically.")]
    public static async Task<string> UpdateScriptReferences(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        [Description("NuGet packages to add. Version is optional — latest stable is used if omitted.")]
        PackageInput[]? addPackages = null,
        [Description("NuGet package IDs to remove (e.g. ['Newtonsoft.Json', 'Dapper'])")]
        string[]? removePackages = null,
        [Description("Assembly file paths to add (e.g. ['/path/to/MyLib.dll'])")]
        string[]? addAssemblies = null,
        [Description("Assembly file paths to remove (e.g. ['/path/to/MyLib.dll'])")]
        string[]? removeAssemblies = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(scriptId, out var id))
        {
            return "Invalid scriptId format. Expected a GUID.";
        }

        if (addPackages is not { Length: > 0 }
            && removePackages is not { Length: > 0 }
            && addAssemblies is not { Length: > 0 }
            && removeAssemblies is not { Length: > 0 })
        {
            return
                "No changes specified. Provide at least one of: addPackages, removePackages, addAssemblies, removeAssemblies.";
        }

        if (addPackages is { Length: > 0 })
        {
            var (resolved, error) =
                await PackageReferenceHelper.ResolveVersionsAsync(api, addPackages, cancellationToken);
            if (error != null)
            {
                return error;
            }

            addPackages = resolved;
        }

        var script = await api.GetScriptAsync(id, cancellationToken);
        var currentRefs = script.Config?.References ?? [];

        var newRefs = new List<ReferenceDto>(currentRefs);
        var changes = new List<string>();

        if (removePackages is { Length: > 0 })
        {
            var removeSet = new HashSet<string>(removePackages, StringComparer.OrdinalIgnoreCase);
            var removed = newRefs.RemoveAll(r =>
                r.Discriminator == ReferenceDto.PackageReferenceDiscriminator
                && r.PackageId != null && removeSet.Contains(r.PackageId));
            if (removed > 0)
            {
                changes.Add($"removed {removed} package(s)");
            }
        }

        if (removeAssemblies is { Length: > 0 })
        {
            var removeSet = new HashSet<string>(removeAssemblies, StringComparer.OrdinalIgnoreCase);
            var removed = newRefs.RemoveAll(r =>
                r.Discriminator == ReferenceDto.AssemblyFileReferenceDiscriminator
                && r.AssemblyPath != null && removeSet.Contains(r.AssemblyPath));

            if (removed > 0)
            {
                changes.Add($"removed {removed} assembly reference(s)");
            }
        }

        if (addPackages is { Length: > 0 })
        {
            foreach (var pkg in addPackages)
            {
                var existing = newRefs.FindIndex(r =>
                    r.Discriminator == ReferenceDto.PackageReferenceDiscriminator
                    && string.Equals(r.PackageId, pkg.Id, StringComparison.OrdinalIgnoreCase));

                if (existing >= 0)
                {
                    if (string.Equals(newRefs[existing].Version, pkg.Version, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    newRefs[existing] = PackageReferenceHelper.CreatePackageRef(pkg);
                    changes.Add($"updated {pkg.Id} to v{pkg.Version}");
                }
                else
                {
                    newRefs.Add(PackageReferenceHelper.CreatePackageRef(pkg));
                    changes.Add($"added {pkg.Id} v{pkg.Version}");
                }
            }
        }

        if (addAssemblies is { Length: > 0 })
        {
            foreach (var asmPath in addAssemblies)
            {
                var alreadyExists = newRefs.Any(r =>
                    r.Discriminator == ReferenceDto.AssemblyFileReferenceDiscriminator
                    && string.Equals(r.AssemblyPath, asmPath, StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                {
                    continue;
                }

                var refDto = PackageReferenceHelper.CreateAssemblyRef(asmPath);
                newRefs.Add(refDto);
                changes.Add($"added assembly {refDto.Title}");
            }
        }

        if (changes.Count == 0)
        {
            return "No changes needed, references already match the requested state.";
        }

        await api.UpdateScriptReferencesAsync(id, newRefs.ToArray(), cancellationToken);

        return $"Script references updated: {string.Join(", ", changes)}.";
    }
}

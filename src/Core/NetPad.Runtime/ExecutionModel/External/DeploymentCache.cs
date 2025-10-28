using System.IO;
using NetPad.IO;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.External;

public class DeploymentCache(DirectoryPath cacheRoot)
{
    public IEnumerable<DeploymentDirectory> ListDeploymentDirectories()
    {
        return Directory.EnumerateDirectories(cacheRoot.Path)
            .Where(d => File.Exists(DeploymentDirectory.GetDeploymentInfoFilePath(d)))
            .Select(d => new DeploymentDirectory(d, false));
    }

    public DeploymentDirectory CreateTempDeploymentDirectory()
    {
        var dirPath = Path.Combine(cacheRoot.Path, Path.GetRandomFileName());
        Directory.CreateDirectory(dirPath);
        return new DeploymentDirectory(dirPath, true);
    }

    public DeploymentDirectory GetOrCreateDeploymentDirectory(Script script, bool forceCreateNew = false)
    {
        // Build cache folders are named like this:
        // {ScriptId}_{ScriptFingerprint}

        // A .netpad file that is formatted correctly will contain a unique guid ID.
        // A file that is not a .netpad script file (a plain text file) will not have an ID. The ID is generated
        // from the file path of that file.

        var cacheRootDir = cacheRoot.GetInfo();
        var scriptId = script.Id.ToString();
        var fingerprint = script.GetFingerprint();
        var fingerprintUuid = fingerprint.CalculateUuid().ToString();
        var expectedDirName = $"{scriptId}_{fingerprintUuid}";
        var expectedDirPath = Path.Combine(cacheRootDir.FullName, expectedDirName);

        if (forceCreateNew)
        {
            if (Directory.Exists(expectedDirPath))
            {
                Directory.Delete(expectedDirPath, true);
            }

            Directory.CreateDirectory(expectedDirPath);
            return new DeploymentDirectory(expectedDirPath, false);
        }

        if (!cacheRootDir.Exists)
        {
            cacheRootDir.Create();
        }

        // First try to find builds for this script (by ID)
        var relatedBuildDirs = cacheRootDir.EnumerateDirectories()
            .Where(p => p.Name.Contains(scriptId) || p.Name.Contains(fingerprintUuid))
            .ToArray();

        if (relatedBuildDirs.Length == 0)
        {
            Directory.CreateDirectory(expectedDirPath);
            return new DeploymentDirectory(expectedDirPath, false);
        }

        var byId = relatedBuildDirs
            .Where(x => x.Name.StartsWith(scriptId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (byId.Length > 0)
        {
            var exactMatch = byId.FirstOrDefault(p => p.Name.Equals(expectedDirName));

            var toDelete = exactMatch == null ? byId : byId.Where(x => x != exactMatch);
            foreach (var buildDir in toDelete)
            {
                Try.Run(() => buildDir.Delete(true));
            }

            if (exactMatch != null)
            {
                return new DeploymentDirectory(exactMatch.FullName, false);
            }
        }

        // Script ID can change example: plain text files that are run with npad CLI base the script ID off file's path which
        // if moved/renamed will change its ID on the next run, but the contents might still be the same.
        // Lookup by fingerprint
        var byFingerprint = cacheRootDir.EnumerateDirectories()
            .Where(p => p.Name.EndsWith(fingerprintUuid))
            .ToArray();

        if (byFingerprint.Length > 0)
        {
            var latest = relatedBuildDirs.OrderByDescending(x => x.CreationTime).First().FullName;
            return new DeploymentDirectory(latest, false);
        }

        // If we cannot find any matches
        Directory.CreateDirectory(expectedDirPath);
        return new DeploymentDirectory(expectedDirPath, false);
    }
}

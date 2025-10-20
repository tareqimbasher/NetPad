using System.IO;
using NetPad.Common;
using NetPad.IO;

namespace NetPad.ExecutionModel.External;

public record DeploymentDirectory(string Path, bool IsTemporary) : DirectoryPath(Path)
{
    private readonly FilePath _deploymentInfoFilePath = GetDeploymentInfoFilePath(Path);

    public static string GetDeploymentInfoFilePath(string directoryPath)
        => System.IO.Path.Combine(directoryPath, ".npad");

    public bool ContainsDeployment => _deploymentInfoFilePath.Exists();

    public FilePath DeploymentInfoFilePath => _deploymentInfoFilePath;

    public DeploymentInfo? GetDeploymentInfo()
    {
        if (!_deploymentInfoFilePath.Exists())
        {
            return null;
        }

        return JsonSerializer.Deserialize<DeploymentInfo>(File.ReadAllText(_deploymentInfoFilePath.Path));
    }

    public void SaveDeploymentInfo(DeploymentInfo deploymentInfo)
    {
        var json = JsonSerializer.Serialize(deploymentInfo);
        File.WriteAllText(DeploymentInfoFilePath.Path, json);
    }

    public long GetSize()
    {
        return GetInfo().EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }
}

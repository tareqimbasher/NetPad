namespace NetPad.Plugins.OmniSharp.Services;

public class OmniSharpServerLocation
{
    public OmniSharpServerLocation(string executablePath)
    {
        ExecutablePath = executablePath;
        DirectoryPath = new FileInfo(executablePath).DirectoryName ??
                        throw new Exception($"Could not get directory path from executable path: {executablePath}");
    }

    public string ExecutablePath { get; }
    public string DirectoryPath { get; }
}

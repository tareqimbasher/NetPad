namespace NetPad.Plugins.OmniSharp.Services;

public class OmniSharpServerLocation(string executablePath)
{
    public string ExecutablePath { get; } = executablePath;
    public string DirectoryPath { get; } = new FileInfo(executablePath).DirectoryName ??
                                           throw new Exception($"Could not get directory path from executable path: {executablePath}");
}

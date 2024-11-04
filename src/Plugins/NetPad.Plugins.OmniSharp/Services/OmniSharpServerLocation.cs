using System.Diagnostics.CodeAnalysis;

namespace NetPad.Plugins.OmniSharp.Services;

public class OmniSharpServerLocation
{
    public OmniSharpServerLocation(string? executablePath, string? entryDllPath = default)
    {
        if (executablePath is null && entryDllPath is null) throw new ArgumentException("Can't locate omnisharp");
        ExecutablePath = executablePath;
        EntryDllPath = entryDllPath;
        DirectoryPath = new FileInfo(executablePath ?? entryDllPath!).DirectoryName ??
                        throw new Exception($"Could not get directory path from executable path: {executablePath}");
    }

    public string? ExecutablePath { get; }
    public string? EntryDllPath { get; }
    public string DirectoryPath { get; }

    public bool Verify()
    {
        return CheckFileExistences(ExecutablePath) || CheckFileExistences(EntryDllPath);
    }

    private static bool CheckFileExistences([NotNullWhen(true)]string? executablePath)
    {
        return !string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath);
    }

}

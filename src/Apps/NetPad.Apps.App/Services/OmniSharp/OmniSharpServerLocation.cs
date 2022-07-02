using System;
using System.IO;

namespace NetPad.Services.OmniSharp;

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

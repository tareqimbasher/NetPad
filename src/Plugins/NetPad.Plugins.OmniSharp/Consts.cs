using NetPad.Configuration;
using NetPad.IO;

namespace NetPad.Plugins.OmniSharp;

public static class Consts
{
    public static readonly DirectoryPath OmniSharpServerProcessesDirectoryPath =
        AppDataProvider.ProcessTempDirectoryPath.Combine("OmniSharp");
}

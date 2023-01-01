using NetPad.Configuration;
using NetPad.IO;

namespace NetPad.Plugins.OmniSharp;

public static class Consts
{
    public static DirectoryPath OmniSharpServerProcessesDirectoryPath = AppDataProvider.TempDirectoryPath.Combine("OmniSharp");
}

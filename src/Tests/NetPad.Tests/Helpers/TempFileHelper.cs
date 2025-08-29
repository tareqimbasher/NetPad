using Xunit;

namespace NetPad.Tests.Helpers;

public static class TempFileHelper
{
    public static string CreateTempTestDirectory<T>() where T : IAsyncLifetime
    {
        var dirPath = Path.Combine(
            Path.GetTempPath(),
            "NetPad_Tests",
            typeof(T).Name,
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(dirPath);
        return dirPath;
    }
}

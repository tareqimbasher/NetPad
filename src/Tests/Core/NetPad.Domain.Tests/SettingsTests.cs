using System;
using System.IO;
using System.Linq;
using NetPad.Configuration;
using Xunit;

namespace NetPad.Domain.Tests;

public class SettingsTests
{
    [Fact]
    public void ScriptsDirectoryPath_Initialized_To_Correct_Directory()
    {
        var settings = new Settings();

        var documentsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Documents",
            "NetPad",
            "Scripts");

        var fallbackDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetPad",
            "Scripts");

        Assert.Contains(new[] { documentsDir, fallbackDir }, e => e == settings.ScriptsDirectoryPath);
    }
}

using System;
using System.IO;
using NetPad.Configuration;
using Xunit;

namespace NetPad.Domain.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void ScriptsDirectoryPath_Initialized_To_Correct_Directory()
        {
            var settings = new Settings();
            var expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "NetPad");

            Assert.Equal(expected, settings.ScriptsDirectoryPath);
        }
    }
}

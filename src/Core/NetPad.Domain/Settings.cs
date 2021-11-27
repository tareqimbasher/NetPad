using System;
using System.IO;

namespace NetPad
{
    public class Settings
    {
        public Settings()
        {
            ScriptsDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "NetPad");
        }

        public string ScriptsDirectoryPath { get; set; }
    }
}

using System;
using System.IO;

namespace NetPad
{
    public class Settings
    {
        public Settings()
        {
            ScriptsDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public string ScriptsDirectoryPath { get; set; }
    }
}

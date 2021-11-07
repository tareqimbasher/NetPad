using System;
using System.IO;

namespace NetPad
{
    public class Settings
    {
        public Settings()
        {
            QueriesDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        
        public string QueriesDirectoryPath { get; set; }
    }
}
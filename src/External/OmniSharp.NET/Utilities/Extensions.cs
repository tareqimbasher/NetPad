using System;
using System.Diagnostics;

namespace OmniSharp.Utilities
{
    internal static class Extensions
    {
        public static bool IsProcessRunning(this Process process)
        {
            try
            {
                return !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}

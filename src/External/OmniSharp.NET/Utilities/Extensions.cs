using System;
using System.Diagnostics;

namespace OmniSharp.Utilities
{
    internal static class Extensions
    {
        public static bool WasProcessStarted(this Process process)
        {
            try
            {
                _ = process.HasExited;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static bool IsProcessRunning(this Process process)
        {
            return process.WasProcessStarted() && !process.HasExited;
        }
    }
}

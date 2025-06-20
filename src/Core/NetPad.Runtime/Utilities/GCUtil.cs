namespace NetPad.Utilities;

public static class GcUtil
{
    /// <summary>
    /// Forces garbage collection and waits for finalizers to complete.
    /// </summary>
    public static void CollectAndWait()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

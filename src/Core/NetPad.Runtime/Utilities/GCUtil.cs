namespace NetPad.Utilities;

public static class GCUtil
{
    public static void CollectAndWait()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

namespace NetPad.Plugins.OmniSharp.Features;

public static class LineCorrecter
{
    public static int AdjustForOmniSharp(int userCodeStartsOnLine, int lineNumber)
    {
        return lineNumber + userCodeStartsOnLine - 1;
    }

    public static OmniSharpPoint AdjustForOmniSharp(int userCodeStartsOnLine, OmniSharpPoint point)
    {
        return new()
        {
            Line = AdjustForOmniSharp(userCodeStartsOnLine, point.Line),
            Column = point.Column
        };
    }

    public static int AdjustForResponse(int userCodeStartsOnLine, int lineNumber)
    {
        return lineNumber - userCodeStartsOnLine + 1;
    }

    public static void AdjustForResponse(int userCodeStartsOnLine, OmniSharpQuickFix quickFix)
    {
        quickFix.Line = AdjustForResponse(userCodeStartsOnLine, quickFix.Line);
        quickFix.EndLine = AdjustForResponse(userCodeStartsOnLine, quickFix.EndLine);
    }

    public static OmniSharpPoint AdjustForResponse(int userCodeStartsOnLine, OmniSharpPoint point)
    {
        return new()
        {
            Line = AdjustForResponse(userCodeStartsOnLine, point.Line),
            Column = point.Column
        };
    }
}

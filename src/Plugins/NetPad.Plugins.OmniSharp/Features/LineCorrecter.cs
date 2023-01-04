namespace NetPad.Plugins.OmniSharp.Features;

[Obsolete("Was used when user code was part of a document that included other code. Since user code is now in its own document, this will be removed soon.")]
public static class LineCorrecter
{
    public static int AdjustForOmniSharp(int userCodeStartsOnLine, int lineNumber)
    {
        return lineNumber + userCodeStartsOnLine - 1;
    }

    public static OmniSharpPoint AdjustForOmniSharp(int userCodeStartsOnLine, OmniSharpPoint point)
    {
        return new OmniSharpPoint
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
        return new OmniSharpPoint
        {
            Line = AdjustForResponse(userCodeStartsOnLine, point.Line),
            Column = point.Column
        };
    }
}

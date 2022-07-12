namespace NetPad.Plugins.OmniSharp.Features.Completion;

public class CompletionItem : OmniSharpCompletionItem
{
    /// <summary>
    /// Hides base Data property. STJ does not know how to deserialize {item1: 0, item2: 1} into a ValueTuple(long, int)
    /// </summary>
    public new CompletionItemData? Data { get; set; }

    public OmniSharpCompletionItem ToOmniSharpCompletionItem()
    {
        var omniSharpCompletionItem = (OmniSharpCompletionItem)this;

        // Copy values from CompletionItem.Data property to base OmniSharpCompletionItem.Data Tuple property
        if (Data != null)
        {
            omniSharpCompletionItem.Data = (Data.Item1, Data.Item2);
        }

        return omniSharpCompletionItem;
    }

    public class CompletionItemData
    {
        public long Item1 { get; set; }
        public int Item2 { get; set; }
    }
}

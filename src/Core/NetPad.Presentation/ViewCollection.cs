using System.Collections.ObjectModel;

namespace NetPad;

// The new LiveCollection
public class ViewCollection<T> : ObservableCollection<T>
{
    public ViewCollection()
    {
        View = new(this);
    }

    public ViewCollection(IEnumerable<T> collection): base(collection)
    {
        View = new(this);
    }

    public CollectionPresentationView<T> View { get; set; }
}

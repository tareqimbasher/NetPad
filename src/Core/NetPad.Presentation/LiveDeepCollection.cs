using System.Collections.Specialized;
using System.ComponentModel;

namespace NetPad;

public sealed class LiveDeepCollection<T> : LiveCollection<T> where T : INotifyPropertyChanged
{
    public LiveDeepCollection()
    {
        CollectionChanged += FullObservableCollectionCollectionChanged;
    }

    public LiveDeepCollection(IEnumerable<T> pItems) : this()
    {
        foreach (var item in pItems)
        {
            Add(item);
        }
    }

    private void FullObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (T item in e.NewItems)
            {
                item.PropertyChanged += ItemPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (T item in e.OldItems)
            {
                item.PropertyChanged -= ItemPropertyChanged;
            }
        }
    }

    private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
        OnCollectionChanged(args);
    }
}

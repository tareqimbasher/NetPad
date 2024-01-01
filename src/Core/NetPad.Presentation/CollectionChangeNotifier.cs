using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NetPad;

public enum CollectionChangeType
{
    CollectionLevelChange, ItemLevelChange
}

public delegate void CollectionChangeHandler();

public class CollectionChangeNotifier<TItem>
{
    private readonly bool _collectionItemsNotifyOnPropertyChange;
    private readonly ObservableCollection<TItem>? _observableCollection;
    private readonly CollectionChangeHandler _changeHandler;
    private readonly IEnumerable<TItem>? _collection;
    private readonly int? _pollMs;
    private bool _shouldSendNotifications;

    public CollectionChangeNotifier(IEnumerable<TItem> collection, int pollMs, CollectionChangeHandler changeHandler) : this(changeHandler)
    {
        _collection = collection;
        _pollMs = pollMs;
    }

    public CollectionChangeNotifier(ObservableCollection<TItem> observableCollection, CollectionChangeHandler changeHandler) : this(changeHandler)
    {
        _observableCollection = observableCollection;
    }

    private CollectionChangeNotifier(CollectionChangeHandler changeHandler)
    {
        _changeHandler = changeHandler;

        _collectionItemsNotifyOnPropertyChange = typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(TItem));
    }

    public async void StartChangeNotifications()
    {
        _shouldSendNotifications = true;

        if (_observableCollection != null)
        {
            _observableCollection.CollectionChanged += OnObservableCollectionChanged;
        }
        else if (_collection != null && _pollMs.HasValue)
        {
            while (_shouldSendNotifications)
            {
                _changeHandler();
                await Task.Delay(_pollMs.Value);
            }
        }
        else
        {
            throw new Exception("Invalid state. Both collections are null.");
        }

        if (_collectionItemsNotifyOnPropertyChange)
        {
            foreach (INotifyPropertyChanged item in (_observableCollection ?? _collection!))
            {
                if (item != null)
                {
                    item.PropertyChanged += ItemPropertyChanged;
                }
            }
        }
    }

    public void StopChangeNotifications()
    {
        _shouldSendNotifications = false;

        if (_observableCollection != null)
        {
            _observableCollection.CollectionChanged -= OnObservableCollectionChanged;
        }

        if (_collectionItemsNotifyOnPropertyChange)
        {
            foreach (INotifyPropertyChanged item in (_observableCollection ?? _collection!))
            {
                if (item != null)
                {
                    item.PropertyChanged -= ItemPropertyChanged;
                }
            }
        }
    }

    private void OnObservableCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_collectionItemsNotifyOnPropertyChange)
        {
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += ItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= ItemPropertyChanged;
                }
            }
        }

        _changeHandler();
    }

    private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
        _changeHandler();
    }
}

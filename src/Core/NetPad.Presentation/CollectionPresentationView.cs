using System.Collections.ObjectModel;

namespace NetPad;

public class CollectionPresentationView<T> : PresentationView<IEnumerable<T>>
{
    private readonly CollectionChangeNotifier<T> _collectionChangeNotifier;

    public CollectionPresentationView(IEnumerable<T> seed, int pollMs) : base(seed)
    {
        _collectionChangeNotifier = new CollectionChangeNotifier<T>(seed, pollMs, () => { DebouncedFlush(); });
    }

    public CollectionPresentationView(ObservableCollection<T> seed) : base(seed)
    {
        _collectionChangeNotifier = new CollectionChangeNotifier<T>(seed, () => { DebouncedFlush(); });
    }

    public int Length { get; private set; }

    public override void StartLiveView(Action<IEnumerable<T>> outputWriter, Func<bool>? stopWhen = null, CancellationToken cancellationToken = default)
    {
        base.StartLiveView(outputWriter, stopWhen, cancellationToken);

        _collectionChangeNotifier.StartChangeNotifications();
    }

    public override void StopLiveView()
    {
        base.StopLiveView();

        _collectionChangeNotifier.StopChangeNotifications();
    }

    private T[]? _lastView;

    public override void UpdateView(IEnumerable<T> data, bool force = false)
    {
        var items = Data.ToArray();

        // if (_lastView != null && items.SequenceEqual(_lastView))
        // {
        //     return;
        // }
        //
        // _lastView = items;

        Length = items.Length;

        base.UpdateView(items);
    }


    /// <summary>
    /// Filters this view by the provided predicate. This alters the view "in-place".
    /// </summary>
    public CollectionPresentationView<T> Where(Func<T, bool> predicate)
    {
        Data = Data.Where(predicate);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> Skip(int count)
    {
        Data = Data.Skip(count);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> Take(int count)
    {
        Data = Data.Take(count);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> Take(Range range)
    {
        Data = Data.Take(range);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        Data = Data.OrderBy(keySelector);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
    {
        Data = Data.OrderByDescending(keySelector);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> DistinctBy<TKey>(Func<T, TKey> keySelector)
    {
        Data = Data.DistinctBy(keySelector);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> UnionBy<TKey>(Func<T, TKey> keySelector, IEnumerable<T> second)
    {
        Data = Data.UnionBy(second, keySelector);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> ExceptBy<TKey>(Func<T, TKey> keySelector, IEnumerable<TKey> second)
    {
        Data = Data.ExceptBy(second, keySelector);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> IntersectBy<TKey>(Func<T, TKey> keySelector, IEnumerable<TKey> second)
    {
        Data = Data.IntersectBy(second, keySelector);
        DebouncedFlush();
        return this;
    }

    public CollectionPresentationView<T> Concat(IEnumerable<T> second)
    {
        Data = Data.Concat(second);
        DebouncedFlush();
        return this;
    }
}

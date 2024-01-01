using System.Collections.ObjectModel;
using System.Collections.Specialized;
using NetPad.Utilities;

namespace NetPad;

public class PollingLiveCollection<T>
{
    private readonly int _pollMs;
    private readonly IEnumerable<T> _collection;

    public PollingLiveCollection(int pollMs, IEnumerable<T> collection)
    {
        _pollMs = pollMs;
        _collection = collection;
    }

    public IEnumerable<T> Items => _collection;

    public void StartLiveView()
    {

    }
}

public static class LiveCollectionExtensions
{
    /// <summary>
    /// Copies this collection into a new <see cref="LiveCollection{T}"/>.
    /// </summary>
    /// <param name="collection">The collection to copy.</param>
    public static LiveCollection<T> ToLiveCollection<T>(this IEnumerable<T> collection)
    {
        return new LiveCollection<T>(collection);
    }
}

/// <summary>
/// A collection that updates its dumped view when changes occur to it. This collection
/// inherits <see cref="ObservableCollection{T}"/> and is a drop-in replacement.
/// <para />
///
/// </summary>
/// <typeparam name="T"></typeparam>
public class LiveCollection<T> : ObservableCollection<T>, IDisposable
{
    private Action<IEnumerable<T>>? _writer;
    private CancellationToken _cancellationToken;
    private readonly Action DebouncedFlush;
    private Func<LiveCollection<T>, bool>? _stopWhen;
    private bool _isListening;
    private IEnumerable<T> _view;

    /// <summary>
    /// Creates a new <see cref="LiveCollection{T}"/>.
    /// </summary>
    public LiveCollection()
    {
        _view = Items;
        DebouncedFlush = new Action(Flush).Debounce(50, immediate: true);
    }

    /// <summary>
    /// Creates a new <see cref="LiveCollection{T}"/> and copies the provided collection into it.
    /// </summary>
    /// <param name="collection">The collection to copy.</param>
    public LiveCollection(IEnumerable<T> collection) : base(collection)
    {
        _view = Items;
        DebouncedFlush = new Action(Flush).Debounce(50, immediate: true);
    }

    /// <summary>
    /// A unique, self-assigned, ID for this collection.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// The "Live View" of the collection.
    /// </summary>
    public IEnumerable<T> View => _view;

    /// <summary>
    /// The current length of items in the "Live View".
    /// </summary>
    public int ViewLength { get; private set; }

    public void StartLiveView(
        Action<IEnumerable<T>> writeToOutput,
        Func<LiveCollection<T>, bool>? stopWhen = null,
        CancellationToken cancellationToken = default)
    {
        _writer = writeToOutput;
        _cancellationToken = cancellationToken;
        _stopWhen = stopWhen;

        if (!_isListening)
        {
            CollectionChanged += ObservableCollectionChanged;
            _isListening = true;
        }

        DebouncedFlush();
    }

    public void StopLiveView()
    {
        CollectionChanged -= ObservableCollectionChanged;
        _isListening = false;
    }

    private void ObservableCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_cancellationToken.IsCancellationRequested || _stopWhen?.Invoke(this) == true)
        {
            StopLiveView();
            return;
        }

        DebouncedFlush();
    }

    /// <summary>
    /// Will flush <see cref="View"/>.
    /// </summary>
    public void Flush()
    {
        if (_writer == null)
        {
            return;
        }

        var items = View.ToArray();
        ViewLength = items.Length;

        _writer(items);
    }

    public void Dispose()
    {
        StopLiveView();
    }

    public LiveCollection<T> Reset()
    {
        _view = Items;
        return this;
    }

    public LiveCollection<T> Where(Func<T, bool> keySelector)
    {
        _view = _view.Where(keySelector);
        return this;
    }

    public LiveCollection<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        _view = _view.OrderBy(keySelector);
        return this;
    }

    public LiveCollection<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
    {
        _view = _view.OrderByDescending(keySelector);
        return this;
    }

    public LiveCollection<T> DistinctBy<TKey>(Func<T, TKey> keySelector)
    {
        _view = _view.DistinctBy(keySelector);
        return this;
    }

    public LiveCollection<T> UnionBy<TKey>(Func<T, TKey> keySelector, IEnumerable<T> second)
    {
        _view = _view.UnionBy(second, keySelector);
        return this;
    }

    public LiveCollection<T> ExceptBy<TKey>(Func<T, TKey> keySelector, IEnumerable<TKey> second)
    {
        _view = _view.ExceptBy(second, keySelector);
        return this;
    }

    public LiveCollection<T> IntersectBy<TKey>(Func<T, TKey> keySelector, IEnumerable<TKey> second)
    {
        _view = _view.IntersectBy(second, keySelector);
        return this;
    }

    public LiveCollection<T> Concat(IEnumerable<T> second)
    {
        _view = _view.Concat(second);
        return this;
    }

    public void AddRange(IEnumerable<T> collection)
    {
        foreach (var i in collection)
        {
            Items.Add(i);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

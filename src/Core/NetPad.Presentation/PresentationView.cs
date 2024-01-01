using System.Collections.Specialized;
using NetPad.Utilities;

namespace NetPad;

// IEnum => ToLiveCollection (created LC) => .View
// Obser => ToLiveCollection (created LC) => .View

// Can represent an object that implements INotifyPropertyChanged or a LiveCollection type object
public class PresentationView<T>
{
    private Action<T>? _outputWriter;
    private CancellationToken _cancellationToken;
    protected readonly Action DebouncedFlush;
    private Func<bool>? _stopWhen;
    private bool _isLiveViewAutoUpdating;

    public PresentationView(T seed)
    {
        Seed = seed;
        Data = seed;

        DebouncedFlush = new Action(() =>
        {
            if (_cancellationToken.IsCancellationRequested || _stopWhen?.Invoke() == true)
            {
                StopLiveView();
                return;
            }

            UpdateView();
        }).Debounce(50, immediate: true);;
    }

    /// <summary>
    /// A unique, self-assigned, ID for this view.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    public T Seed { get; set; }
    public T Data { get; set; }


    public virtual void StartLiveView(
        Action<T> outputWriter,
        Func<bool>? stopWhen = null,
        CancellationToken cancellationToken = default)
    {
        _outputWriter = outputWriter;
        _cancellationToken = cancellationToken;
        _stopWhen = stopWhen;

        _isLiveViewAutoUpdating = true;

        DebouncedFlush();
    }

    public virtual void StopLiveView()
    {
        UpdateView();

        _isLiveViewAutoUpdating = false;
    }

    /// <summary>
    /// Resets the view to its original state, removing any filters or transformations made to the view.
    /// </summary>
    /// <returns></returns>
    public PresentationView<T> Reset()
    {
        Data = Seed;
        DebouncedFlush();
        return this;
    }

    public void UpdateView() => UpdateView(Data);

    public virtual void UpdateView(T data, bool force = false)
    {
        _outputWriter?.Invoke(data);
    }
}

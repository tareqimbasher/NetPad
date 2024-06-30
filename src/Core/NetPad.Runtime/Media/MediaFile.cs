using System.Diagnostics;
using System.IO;

namespace NetPad.Media;

/// <summary>
/// Base class for media files of type T.
/// </summary>
public abstract class MediaFile<T> : MediaFile where T : MediaFile, new()
{
    protected MediaFile()
    {
    }

    protected MediaFile(string filePath) : base(filePath)
    {
    }

    protected MediaFile(Uri uri) : base(uri)
    {
    }

    /// <summary>
    /// Creates a new file from a local file path.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    public static T FromPath(string filePath) => new T { FilePath = filePath };

    /// <summary>
    /// Creates a new file from a URI (link).
    /// </summary>
    /// <param name="uri">The uri that points to the file.</param>
    public static T FromUri(Uri uri) => new T { Uri = uri };

    /// <summary>
    /// Creates a new file from a HTML-compatible Base64 string. Example: "data:image/png;base64,9j/4AAQS...".
    /// </summary>
    /// <param name="base64"></param>
    public static T FromBase64(string base64) => new T { Base64Data = base64 };

    /// <summary>
    /// Creates a new file from a byte array. This is a convenience method and will call <see cref="FromBase64"/> internally.
    /// </summary>
    /// <param name="bytes">Data bytes.</param>
    /// <param name="mimeType">The mimetype of the data.</param>
    public static T FromBytes(byte[] bytes, string mimeType)
        => FromBase64($"data:{mimeType};base64,{Convert.ToBase64String(bytes)}");
}

/// <summary>
/// Base class for media files.
/// </summary>
public abstract class MediaFile
{
    private readonly string? _base64Data;
    private readonly string? _filePath;
    private readonly Uri? _uri;
    private string? _htmlSource;

    protected MediaFile()
    {
    }

    protected MediaFile(string filePath)
    {
        FilePath = filePath;
    }

    protected MediaFile(Uri uri)
    {
        Uri = uri;
        _htmlSource = Uri.ToString();
    }

    /// <summary>
    /// The width in which this media file should be displayed.
    /// </summary>
    public string? DisplayWidth { get; set; }

    /// <summary>
    /// The height in which this media file should be displayed.
    /// </summary>
    public string? DisplayHeight { get; set; }

    /// <summary>
    /// The path to the file this media file represents. Will be null if file is not local.
    /// </summary>
    public string? FilePath
    {
        get => _filePath;
        init
        {
            _filePath = value;
            UpdateHtmlSource();
        }
    }

    /// <summary>
    /// The Base64 encoded data representing the contents of this media file.
    /// </summary>
    public string? Base64Data
    {
        get => _base64Data;
        init
        {
            _base64Data = value;
            UpdateHtmlSource();
        }
    }

    /// <summary>
    /// The URI from where this media file is loaded.
    /// </summary>
    public Uri? Uri
    {
        get => _uri;
        init
        {
            _uri = value;
            UpdateHtmlSource();
        }
    }

    /// <summary>
    /// Indicates if this file points to a local file. Returns true only if <see cref="FilePath"/> is not null.
    /// </summary>
    public bool IsLocalFile => FilePath != null;

    /// <summary>
    /// The HTML source used to load this media file in an HTML element.
    /// </summary>
    public string HtmlSource => _htmlSource ?? string.Empty;

    private void UpdateHtmlSource()
    {
        if (FilePath != null)
        {
            _htmlSource = $"/files/{Uri.EscapeDataString(FilePath)}";
        }
        else if (Uri != null)
        {
            _htmlSource = Uri.ToString();
        }
        else if (Base64Data != null)
        {
            _htmlSource = Base64Data;
        }
        else
        {
            _htmlSource = null;
        }
    }

    /// <summary>
    /// Opens file with the default application. Does not wait for spawned process to exit.
    /// </summary>
    /// <exception cref="InvalidOperationException">Will be thrown if used on a file that only has <see cref="Base64Data"/>.</exception>
    /// <exception cref="FileNotFoundException">If the file could not be opened.</exception>
    public void Open()
    {
        if (FilePath == null && Uri == null && Base64Data != null)
        {
            throw new InvalidOperationException($"Cannot open a {GetType().Name} with only {nameof(Base64Data)}.");
        }

        var path = FilePath != null && File.Exists(FilePath) ? FilePath : HtmlSource;

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Not enough info to open the {GetType().Name} file.");
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });

        if (process == null)
        {
            throw new FileNotFoundException($"Could not open {GetType().Name} file.");
        }
    }

    /// <summary>
    /// Opens file with the default application and then waits for the spawned process to exit.
    /// </summary>
    /// <exception cref="InvalidOperationException">Will be thrown if used on a file that only has <see cref="Base64Data"/>.</exception>
    /// <exception cref="FileNotFoundException">If the file could not be opened.</exception>
    public void OpenAndWait()
    {
        if (FilePath == null && Uri == null && Base64Data != null)
        {
            throw new InvalidOperationException($"Cannot open a {GetType().Name} with only {nameof(Base64Data)}.");
        }

        var path = FilePath != null && File.Exists(FilePath) ? FilePath : HtmlSource;

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Not enough info to open the {GetType().Name} file.");
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });

        if (process == null)
        {
            throw new FileNotFoundException($"Could not open {GetType().Name} file.");
        }

        process.WaitForExit();
    }
}

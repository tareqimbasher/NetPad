using System.Diagnostics;
using NetPad.IO;

namespace NetPad.Media;

/// <summary>
/// Base class for media files of type <see cref="T"/>.
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
    /// <returns></returns>
    public static T FromUri(Uri uri) => new T { Uri = uri };

    /// <summary>
    /// Creates a new file from a HTML-compatible Base64 string. Example: "data:image/png;base64,9j/4AAQS...".
    /// </summary>
    /// <param name="base64"></param>
    /// <returns></returns>
    public static T FromBase64(string base64) => new T { Base64Data = base64 };
}

/// <summary>
/// Base class for media files.
/// </summary>
public abstract class MediaFile
{
    private readonly string? _base64Data;
    private readonly FilePath? _filePath;
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

    public FilePath? FilePath
    {
        get => _filePath;
        init
        {
            _filePath = value;
            UpdateHtmlSource();
        }
    }

    public string? Base64Data
    {
        get => _base64Data;
        init
        {
            _base64Data = value;
            UpdateHtmlSource();
        }
    }

    public Uri? Uri
    {
        get => _uri;
        init
        {
            _uri = value;
            UpdateHtmlSource();
        }
    }

    private void UpdateHtmlSource()
    {
        if (FilePath != null)
        {
            _htmlSource = $"/files/{Uri.EscapeDataString(FilePath.Path)}";
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
    /// Indicates if this file points to a local file. Returns true if <see cref="FilePath"/> is not null.
    /// </summary>
    public bool IsLocalFile => FilePath != null;

    public string HtmlSource => _htmlSource ?? string.Empty;

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

        var path = FilePath?.Exists() == true ? FilePath.Path : HtmlSource;

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

        var path = FilePath?.Exists() == true ? FilePath.Path : HtmlSource;

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

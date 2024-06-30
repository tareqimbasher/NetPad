namespace NetPad.Media;

/// <summary>
/// Represents a dumpable audio file.
/// </summary>
public class Audio : MediaFile<Audio>
{
    public Audio()
    {
    }

    public Audio(string filePath) : base(filePath)
    {
    }

    public Audio(Uri uri) : base(uri)
    {
    }
}
